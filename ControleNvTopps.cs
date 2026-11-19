using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

internal static class Program
{
    private const string Service = @"SYSTEM\CurrentControlSet\Services\NVDisplay.ContainerLocalSystem";
    private const string Plugin = Service + @"\Parameters\LocalSystem\_nvtopps";
    private const string Setting = "DisableLoad";

    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length != 0)
        {
            if (args.Length != 1 || (args[0] != "--ativar" && args[0] != "--desativar")) return 2;
            using (var identity = WindowsIdentity.GetCurrent())
            {
                if (!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator)) return 3;
            }

            try
            {
                bool enable = args[0] == "--ativar";
                SetSetting(enable);
                return ReadSetting() == enable ? 0 : 4;
            }
            catch (Exception)
            {
                return 4;
            }
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new ControlWindow());
        return 0;
    }

    internal static bool ReadSetting()
    {
        using (var hive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
        using (var service = hive.OpenSubKey(Service))
        {
            if (service == null) throw new InvalidOperationException("O serviço NVIDIA Display Container LS não foi encontrado.");

            using (var plugin = hive.OpenSubKey(Plugin))
            {
                object value = plugin == null ? null : plugin.GetValue(Setting);
                if (value == null) return false;
                if (!(value is int) || ((int)value != 0 && (int)value != 1))
                {
                    throw new InvalidOperationException("O valor DisableLoad está em um formato inesperado.");
                }
                return (int)value == 1;
            }
        }
    }

    private static void SetSetting(bool enabled)
    {
        if (ReadSetting() == enabled) return;

        using (var hive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
        using (var plugin = enabled ? hive.CreateSubKey(Plugin) : hive.OpenSubKey(Plugin, true))
        {
            if (plugin == null) throw new InvalidOperationException("A chave do plugin não está disponível.");
            if (enabled)
                plugin.SetValue(Setting, 1, RegistryValueKind.DWord);
            else
                plugin.DeleteValue(Setting, false);
        }
    }
}

internal sealed class ControlWindow : Form
{
    private static readonly Color Background = Color.FromArgb(18, 25, 30);
    private static readonly Color Surface = Color.FromArgb(29, 40, 47);
    private static readonly Color Foreground = Color.FromArgb(244, 247, 248);
    private static readonly Color Secondary = Color.FromArgb(182, 198, 204);
    private static readonly Color Mint = Color.FromArgb(152, 234, 193);
    private static readonly Color Amber = Color.FromArgb(252, 206, 127);

    private readonly Label status;
    private readonly Label guidance;
    private readonly Button activate;
    private readonly Button deactivate;
    private bool working;

    internal ControlWindow()
    {
        Text = "Controle do log NVIDIA";
        Font = new Font("Segoe UI", 10f);
        BackColor = Background;
        ForeColor = Foreground;
        ClientSize = new Size(530, 337);
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        AddLabel(this, "Controle do log NVIDIA", 27, 22, 475, 42, 22, Foreground, true);
        AddLabel(this, "Ative a solução para bloquear o plugin que grava o log.", 30, 71, 470, 25, 10, Secondary);

        var card = new Panel { Location = new Point(30, 111), Size = new Size(470, 88), BackColor = Surface };
        Controls.Add(card);
        AddLabel(card, "Configuração salva no Registro", 17, 13, 430, 23, 10, Secondary);
        status = AddLabel(card, "Consultando…", 17, 39, 430, 35, 17, Foreground, true);

        activate = AddButton("&Ativar", 30, Mint, Background);
        deactivate = AddButton("&Desativar", 272, Surface, Foreground);
        activate.AccessibleDescription = "Impede o carregamento do plugin NvTopps após reiniciar o Windows.";
        deactivate.AccessibleDescription = "Restaura o carregamento padrão do plugin NvTopps após reiniciar o Windows.";
        activate.Click += async (sender, args) => await ChangeSetting(true);
        deactivate.Click += async (sender, args) => await ChangeSetting(false);
        guidance = AddLabel(this, "", 30, 273, 470, 47, 10, Secondary);

        RefreshState("O Windows pode pedir permissão de administrador.\nA mudança entra em vigor após reiniciar.");
    }

    private static Label AddLabel(Control parent, string text, int x, int y, int width, int height, float size, Color color, bool bold = false)
    {
        var label = new Label
        {
            Text = text, Location = new Point(x, y), Size = new Size(width, height),
            Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
            ForeColor = color, UseMnemonic = false
        };
        parent.Controls.Add(label);
        return label;
    }

    private Button AddButton(string text, int x, Color fill, Color ink)
    {
        var button = new Button
        {
            Text = text, Location = new Point(x, 216), Size = new Size(228, 43),
            Font = new Font("Segoe UI", 11, FontStyle.Bold), FlatStyle = FlatStyle.Flat,
            BackColor = fill, ForeColor = ink, UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(91, 112, 121);
        Controls.Add(button);
        return button;
    }

    private void RefreshState(string message)
    {
        try
        {
            bool active = Program.ReadSetting();
            status.Text = active ? "Solução ativada" : "Solução desativada";
            status.ForeColor = active ? Mint : Foreground;
            activate.Enabled = !working && !active;
            deactivate.Enabled = !working && active;
            guidance.Text = message;
        }
        catch (Exception error)
        {
            status.Text = "Estado indisponível";
            status.ForeColor = Amber;
            activate.Enabled = false;
            deactivate.Enabled = false;
            guidance.Text = error.Message;
        }
    }

    private async Task ChangeSetting(bool enable)
    {
        if (working) return;
        working = true;
        RefreshState("Aguardando autorização do Windows…");
        string message;

        try
        {
            int result = await Task.Run(() =>
            {
                var start = new ProcessStartInfo(Application.ExecutablePath, enable ? "--ativar" : "--desativar")
                {
                    UseShellExecute = true,
                    Verb = "runas"
                };
                using (var process = Process.Start(start))
                {
                    if (process == null) return 4;
                    process.WaitForExit();
                    return process.ExitCode;
                }
            });
            message = result == 0 && Program.ReadSetting() == enable
                ? "Configuração salva. Reinicie o Windows para aplicar."
                : "Não foi possível confirmar a alteração. Confira o estado acima.";
        }
        catch (Win32Exception error)
        {
            message = error.NativeErrorCode == 1223
                ? "Autorização cancelada. Você pode tentar novamente."
                : "O Windows não conseguiu iniciar a alteração.";
        }
        catch (Exception)
        {
            message = "Não foi possível confirmar a alteração. Confira o estado acima.";
        }

        working = false;
        if (!IsDisposed) RefreshState(message);
    }
}
