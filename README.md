# NVIDIA Fix Disk

[English documentation](README.en.md) · [Código-fonte](ControleNvTopps.cs) · [Baixar pela GitHub Release](https://github.com/luisbarrelin/nvidia-fix-disk/releases/latest)

Utilitário portátil para Windows que aplica uma solução temporária para as regravações repetidas de `nvlog.nvlgstg` observadas no driver GeForce 621.24. A ferramenta permite manter o driver instalado e impedir que o serviço **NVIDIA Display Container LS** carregue o plugin `_nvtopps.dll`. É um projeto independente da comunidade.

![Tela do utilitário com a solução ativada e os botões Ativar e Desativar](screenshot.png)

## Usar

1. Baixe `NVIDIA-Log-Control.exe` na release. O executável x64 é distribuído sem assinatura digital. É possível conferir o SHA-256 ou compilar pelo código-fonte.
2. Abra o executável. A janela mostra o estado **salvo no Registro** e pede autorização de administrador ao alterar a configuração.
3. Clique em **Ativar**, conceda a autorização do Windows e reinicie o computador. A janela informa quando o valor foi salvo. O serviço usa esse valor no próximo carregamento.
4. Para desfazer, clique em **Desativar**, autorize a alteração e reinicie novamente. Depois que uma versão corrigida do driver estiver instalada, esse é o caminho para restaurar o carregamento padrão do plugin.

### Verificar o executável

Na pasta em que salvou os arquivos, compare o hash retornado com o conteúdo de `SHA256SUMS.txt` da mesma release:

```powershell
Get-FileHash -LiteralPath '.\NVIDIA-Log-Control.exe' -Algorithm SHA256
```

## O que a ferramenta altera

A interface em C# e Windows Forms lê o Registro ao abrir. Cada clique de alteração inicia uma segunda instância do próprio executável com elevação via UAC. Essa instância grava ou remove um único valor e verifica o resultado. A interface faz a leitura novamente ao final.

```text
Chave  HKLM\SYSTEM\CurrentControlSet\Services\NVDisplay.ContainerLocalSystem\Parameters\LocalSystem\_nvtopps
Valor  DisableLoad (REG_DWORD)
```

| Ação | Efeito |
| --- | --- |
| Ativar | Define `DisableLoad` como `1`, criando a chave do plugin se necessário. |
| Desativar | Remove `DisableLoad` quando está em `1`. A chave pode permanecer vazia. |

O código preserva valores inesperados e apresenta erro de leitura. Ele não reinicia o serviço ou o Windows, não exclui logs ou arquivos do driver, não instala componentes, não cria tarefas agendadas e não faz conexões de rede. O driver e o serviço permanecem instalados. Os dois argumentos internos `--ativar` e `--desativar` exigem uma execução já elevada e retornam um código de saída para a interface.

## Como confirmar depois do reinício

Execute em PowerShell:

```powershell
$key = 'HKLM:\SYSTEM\CurrentControlSet\Services\NVDisplay.ContainerLocalSystem\Parameters\LocalSystem\_nvtopps'
Get-ItemPropertyValue -LiteralPath $key -Name DisableLoad

$serviceLog = Join-Path $env:ProgramData 'NVIDIA\NVDisplay.ContainerLocalSystem.log'
if (Test-Path -LiteralPath $serviceLog) {
    Select-String -LiteralPath $serviceLog -Pattern 'Disable loading of plugin.*_nvtopps\.dll' |
        Select-Object -Last 1
}

$logDir = Join-Path $env:ProgramData 'NVIDIA Corporation\nvtopps\nct'
if (Test-Path -LiteralPath $logDir) {
    Get-ChildItem -LiteralPath $logDir -Force | Select-Object Name, Length, LastWriteTime
}
```

Com a solução ativa, espere `1` no Registro. Se o log do serviço estiver disponível, procure uma entrada do **reinício mais recente** indicando que o plugin deixou de carregar. Verifique após um intervalo se o `nvlog.nvlgstg` continua ausente ou sem novas alterações. Arquivos de log antigos podem permanecer. O crescimento de um arquivo mede tamanho líquido. Para observar gravações, examine a atividade de disco no Monitor de Recursos e filtre pelo nome do arquivo.

## Validação e limitações

- **Verificado em uma máquina** com Windows 11, GeForce RTX 3090 e driver 621.24. Depois do reboot, `DisableLoad` continuou em `1`, o serviço registrou que ignorou o plugin e o arquivo ativo estava ausente ao fim de oito minutos. Dois logs antigos ficaram estáveis durante um minuto de amostragem. O driver continuou na versão 621.24.
- A janela foi verificada quanto à abertura e à leitura do estado. A alteração no Registro e o efeito pós-reinício foram verificados separadamente. O fluxo completo dos dois botões com UAC e reversão ainda não passou por teste de ponta a ponta.
- A mudança pode reduzir os registros de diagnóstico associados ao plugin. Não há uma matriz de testes para jogos, CUDA, DLSS, G-SYNC, NVIDIA App ou ajuste automático da GPU. Teste os recursos que usa e reverta se observar uma regressão.
- O defeito também foi relatado em 621.14. O projeto não confirma o comportamento em outros drivers, PCs ou versões do Windows. Uma atualização do driver pode mudar a chave, o funcionamento do plugin ou a necessidade da solução.

## Compilar

Requisitos: Windows x64 com .NET Framework e o compilador `csc.exe` de 64 bits. A compilação usa somente bibliotecas do .NET Framework e não depende de pacotes NuGet. Execute na raiz do projeto:

```powershell
powershell.exe -NoProfile -File .\build.ps1
```

O resultado é `build\NVIDIA-Log-Control.exe`. O script usa a mesma configuração do compilador utilizada na primeira release. Compilações locais podem gerar hashes diferentes.

Para relatar um problema, inclua versão do Windows, modelo da GPU, versão do driver, resultado após reiniciar e etapas para reproduzir. Remova identificadores, nomes de usuário e caminhos pessoais antes de anexar logs.

## Referências e licença

- [Página do driver GeForce 621.24](https://www.nvidia.com/en-us/geforce/drivers/details/285321/)
- [Discussão do problema no fórum GeForce](https://www.nvidia.com/en-us/geforce/forums/game-ready-drivers/13/592907/continuous-writing-of-gigabytes-to-ssd-to-nvtopps-/)
- [Relato do ajuste no Registro e verificação com Process Monitor](https://old.reddit.com/r/nvidia/comments/1xuef1f/comment/19su1doggqogy/)
- [Licença MIT](LICENSE)
