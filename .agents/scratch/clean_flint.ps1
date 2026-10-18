$regPath = "HKCU:\Software\Unity\UnityEditor\DefaultCompany\My project"
$cleanVal = [System.Text.Encoding]::UTF8.GetBytes("SecretCode;SecretBook" + [char]0)
Set-ItemProperty -Path $regPath -Name "SavedInventory_ItemIDs_h50959134" -Value $cleanVal
Write-Output "Cleaned registry successfully."
