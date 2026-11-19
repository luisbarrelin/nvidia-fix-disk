using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyFileVersion("1.1.0.0")]
[assembly: AssemblyInformationalVersion("1.1.0")]

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
            if (service == null) throw new SettingReadException(true);

            using (var plugin = hive.OpenSubKey(Plugin))
            {
                object value = plugin == null ? null : plugin.GetValue(Setting);
                if (value == null) return false;
                if (!(value is int) || ((int)value != 0 && (int)value != 1))
                {
                    throw new SettingReadException(false);
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
            if (plugin == null) throw new InvalidOperationException("The plugin registry key is unavailable.");
            if (enabled)
                plugin.SetValue(Setting, 1, RegistryValueKind.DWord);
            else
                plugin.DeleteValue(Setting, false);
        }
    }
}

internal sealed class SettingReadException : Exception
{
    internal readonly bool MissingService;

    internal SettingReadException(bool missingService)
    {
        MissingService = missingService;
    }
}

internal sealed class UiText
{
    internal string Title, Intro, Registry, Reading, Enabled, Disabled, Enable, Undo, EnableHint, UndoHint;
    internal string Idle, Waiting, Saved, Cancelled, Failed, Unavailable, MissingService, UnexpectedValue, Unreadable;

    internal static UiText ForCulture(CultureInfo culture)
    {
        switch (culture.TwoLetterISOLanguageName)
        {
            case "pt": return Portuguese;
            case "zh": return Chinese;
            default: return English;
        }
    }

    private static readonly UiText English = new UiText
    {
        Title = "NVIDIA Log Control",
        Intro = "Temporarily stop repeated NVIDIA log writes while keeping your current graphics driver.",
        Registry = "Saved setting",
        Reading = "Reading…",
        Enabled = "Workaround on",
        Disabled = "Workaround off",
        Enable = "&Turn on workaround",
        Undo = "&Restore default",
        EnableHint = "Prevents the NvTopps plugin from loading after Windows restarts.",
        UndoHint = "Restores the default NvTopps plugin behavior after Windows restarts.",
        Idle = "Windows may ask for administrator permission. Restart Windows after changing the setting.",
        Waiting = "Waiting for Windows approval…",
        Saved = "Setting saved. Restart Windows to apply it.",
        Cancelled = "Approval canceled. You can try again.",
        Failed = "The change could not be confirmed. Check the saved setting above.",
        Unavailable = "Setting unavailable",
        MissingService = "NVIDIA Display Container LS was not found on this computer.",
        UnexpectedValue = "The registry value has an unexpected format or value.",
        Unreadable = "Windows could not read the NVIDIA registry setting."
    };

    private static readonly UiText Portuguese = new UiText
    {
        Title = "Controle do log NVIDIA",
        Intro = "Interrompa temporariamente as regravações do log da NVIDIA e mantenha o driver de vídeo atual.",
        Registry = "Configuração salva",
        Reading = "Consultando…",
        Enabled = "Solução ativada",
        Disabled = "Solução desativada",
        Enable = "&Ativar solução",
        Undo = "&Restaurar padrão",
        EnableHint = "Impede que o plugin NvTopps seja carregado após reiniciar o Windows.",
        UndoHint = "Restaura o comportamento padrão do plugin NvTopps após reiniciar o Windows.",
        Idle = "O Windows pode pedir autorização de administrador. Reinicie o Windows após alterar a configuração.",
        Waiting = "Aguardando autorização do Windows…",
        Saved = "Configuração salva. Reinicie o Windows para aplicar.",
        Cancelled = "Autorização cancelada. Você pode tentar novamente.",
        Failed = "Não foi possível confirmar a alteração. Confira a configuração salva acima.",
        Unavailable = "Configuração indisponível",
        MissingService = "O serviço NVIDIA Display Container LS não foi encontrado neste computador.",
        UnexpectedValue = "O valor no Registro contém um formato ou dado inesperado.",
        Unreadable = "O Windows não conseguiu ler a configuração da NVIDIA no Registro."
    };

    private static readonly UiText Chinese = new UiText
    {
        Title = "NVIDIA 日志控制",
        Intro = "临时停止 NVIDIA 日志的重复写入，同时保留当前显卡驱动程序。",
        Registry = "已保存的设置",
        Reading = "正在读取…",
        Enabled = "临时方案已启用",
        Disabled = "临时方案未启用",
        Enable = "启用临时方案",
        Undo = "恢复默认设置",
        EnableHint = "重新启动 Windows 后阻止加载 NvTopps 插件。",
        UndoHint = "重新启动 Windows 后恢复 NvTopps 插件的默认行为。",
        Idle = "Windows 可能会请求管理员权限。更改设置后，请重新启动 Windows。",
        Waiting = "正在等待 Windows 授权…",
        Saved = "设置已保存。请重新启动 Windows 以应用更改。",
        Cancelled = "授权已取消。您可以重试。",
        Failed = "无法确认更改。请查看上方已保存的设置。",
        Unavailable = "无法读取设置",
        MissingService = "在此电脑上找不到 NVIDIA Display Container LS 服务。",
        UnexpectedValue = "注册表中的值或格式不符合预期。",
        Unreadable = "Windows 无法读取 NVIDIA 注册表设置。"
    };
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
    private readonly Button action;
    private readonly UiText labels;
    private bool active;
    private bool working;

    internal ControlWindow()
    {
        labels = UiText.ForCulture(CultureInfo.CurrentUICulture);
        Text = labels.Title;
        Font = new Font("Segoe UI", 10f);
        BackColor = Background;
        ForeColor = Foreground;
        ClientSize = new Size(498, 329);
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        AddLabel(this, labels.Title, 25, 20, 448, 46, 22, Foreground, true);
        AddLabel(this, labels.Intro, 28, 69, 442, 44, 10, Secondary);

        var card = new Panel { Location = new Point(28, 123), Size = new Size(442, 76), BackColor = Surface };
        Controls.Add(card);
        AddLabel(card, labels.Registry, 15, 10, 412, 23, 10, Secondary);
        status = AddLabel(card, labels.Reading, 15, 34, 412, 34, 16, Foreground, true);

        action = new Button
        {
            Location = new Point(28, 213), Size = new Size(442, 45),
            Font = new Font("Segoe UI", 11, FontStyle.Bold), FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false
        };
        action.FlatAppearance.BorderColor = Color.FromArgb(91, 112, 121);
        Controls.Add(action);
        action.Click += async (sender, args) => await ChangeSetting(!active);
        guidance = AddLabel(this, "", 28, 271, 442, 48, 10, Secondary);

        RefreshState(labels.Idle);
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

    private void RefreshState(string message)
    {
        try
        {
            active = Program.ReadSetting();
            status.Text = active ? labels.Enabled : labels.Disabled;
            status.ForeColor = active ? Mint : Foreground;
            action.Text = active ? labels.Undo : labels.Enable;
            action.AccessibleDescription = active ? labels.UndoHint : labels.EnableHint;
            action.BackColor = active ? Surface : Mint;
            action.ForeColor = active ? Foreground : Background;
            action.Enabled = !working;
            guidance.Text = message;
        }
        catch (SettingReadException error)
        {
            ShowReadError(error.MissingService ? labels.MissingService : labels.UnexpectedValue);
        }
        catch (Exception)
        {
            ShowReadError(labels.Unreadable);
        }
    }

    private void ShowReadError(string message)
    {
        status.Text = labels.Unavailable;
        status.ForeColor = Amber;
        action.Text = labels.Enable;
        action.Enabled = false;
        guidance.Text = message;
    }

    private async Task ChangeSetting(bool enable)
    {
        if (working) return;
        working = true;
        RefreshState(labels.Waiting);
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
                ? labels.Saved
                : labels.Failed;
        }
        catch (Win32Exception error)
        {
            message = error.NativeErrorCode == 1223 ? labels.Cancelled : labels.Failed;
        }
        catch (Exception)
        {
            message = labels.Failed;
        }

        working = false;
        if (!IsDisposed) RefreshState(message);
    }
}
