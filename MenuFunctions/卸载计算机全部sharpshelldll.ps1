# 自动提权部分
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "需要管理员权限，正在尝试以管理员身份重新启动..."
    Start-Process powershell -ArgumentList "-ExecutionPolicy Bypass -File `"$PSCommandPath`"" -Verb RunAs
    exit
}

Write-Host "==============================="
Write-Host " 正在清理 SharpShell 残留注册项..."
Write-Host "==============================="

# 自动结束 Explorer
Write-Host ""
Write-Host "正在结束 explorer.exe..."
Get-Process explorer -ErrorAction SilentlyContinue | Stop-Process -Force

$views = @("Registry64", "Registry32")

foreach ($view in $views) {
    Write-Host ""
    Write-Host "[$view] 清理中..."

    # 打开 HKCR\CLSID，找 InprocServer32 指向 mscoree.dll 且带 SharpShell 信息的项
    $base = [Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::ClassesRoot, $view)
    $clsid = $base.OpenSubKey("CLSID",$true)
    if ($clsid -eq $null) {
        Write-Host "  未找到 CLSID 节点。"
        continue
    }

    foreach ($sub in $clsid.GetSubKeyNames()) {
        if ($sub -notmatch '^{.*}$') { continue }

        try {
            $key = $clsid.OpenSubKey($sub,$false)
            if ($null -eq $key) { continue }

            $inproc = $key.OpenSubKey("InprocServer32",$false)
            if ($null -ne $inproc) {
                $asm  = $inproc.GetValue("Assembly")
                $code = $inproc.GetValue("CodeBase")

                if (($asm -and $asm -like "*SharpShell*") -or ($code -and $code -like "*SharpShell*")) {
                    try {
                        $clsid.DeleteSubKeyTree($sub,$false)
                        Write-Host "  删除 HKCR\CLSID\$sub"
                    } catch {
                        Write-Host ("  删除失败 {0}: {1}" -f $sub, $_.Exception.Message)
                    }
                }
                $inproc.Close()
            }
            $key.Close()
        } catch {
            Write-Host ("  遍历出错 {0}: {1}" -f $sub, $_.Exception.Message)
        }
    }
    $clsid.Close()
    $base.Close()

    # 清理 Approved 列表
    Write-Host "  清理 Shell Extensions\Approved..."
    $lm = [Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::LocalMachine, $view)
    $approved = $lm.OpenSubKey("Software\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved",$true)
    if ($approved) {
        foreach ($v in $approved.GetValueNames()) {
            $val = $approved.GetValue($v)
            if ($val -like "*SharpShell*" -or $v -like "*SharpShell*") {
                try {
                    $approved.DeleteValue($v,$false)
                    Write-Host "    删除 Approved 值：$v"
                } catch {
                    Write-Host ("    删除失败 {0}: {1}" -f $v, $_.Exception.Message)
                }
            }
        }
        $approved.Close()
    }
    $lm.Close()
}

Write-Host ""
Write-Host "正在重新启动 explorer.exe..."
Start-Process "explorer.exe"

Write-Host ""
Write-Host "===================================="
Write-Host " SharpShell 清理完成 ✅"
Write-Host " 已自动重启资源管理器。"
Write-Host "===================================="
Start-Sleep -Seconds 3
