Param(
	[Parameter(Mandatory=$true)][string]$serverName,
    [Parameter(Mandatory=$true)][string]$userName,
    [Parameter(Mandatory=$true)][string]$password,
    [switch]$isSsl=$false,
	[switch]$isOnline=$false
)

$u ; $p ; $s ; $uri
$cloudUri = "https://ps.outlook.com/Powershell"
$auth = "Default"
if ($isOnline -eq $true) {
	$auth = "Basic"
}
$prefix = "http"
if ($isSsl) {
    $prefix = "https"
}
$uri = $prefix + "://" + $serverName + "/Powershell/"

$psec = ConvertTo-SecureString $password -AsPlainText -Force
$c = New-Object System.Management.Automation.PSCredential ($userName, $psec)

#cd WSMan:\localhost\Client
#dir
#set-item .\allowunencrypted $true
if ($isOnline -eq $false) {
	set-item WSMAN:\localhost\Client\allowunencrypted $true
}

#$so = New-PSSessionOption -MaxConnectionRetryCount 10 -MaximumRedirection 30 -SkipCACheck -SkipCNCheck -SkipRevocationCheck
$so = New-PSSessionOption -SkipCACheck -SkipCNCheck -SkipRevocationCheck
$conf = "Microsoft.Exchange"

"User is $userName"
"Configuration name $conf, using the $auth authentication method"
"Connecting to $uri..."

$s = New-PSSession -Credential $c -SessionOption $so -ConfigurationName $conf -URI $uri -AllowRedirection -Authentication $auth

if ($s -ne $null) {
	Import-PSSession $s
	#Remove-PSSession $s

	Set-ExecutionPolicy -ExecutionPolicy ByPass -Scope Process -Force
}
""
"               Account Name    email        mailbox version"
"---------------------------- -------------- ---------------"
"Getting mailboxes"
#get-mailbox | select name,primarysmtpaddress,exchangeversion | Format-Table -GroupBy ExchangeVersion
get-mailbox -ResultSize Unlimited | select name,primarysmtpaddress,exchangeversion | SORT-Object -Property ExchanageVersion -Descending | Foreach-Object {
"Testing mailbox $($_.name), $($_.primarysmtpaddress), $($_.ExchangeVersion)"
Search-MailboxAuditLog -Identity $($_.primarysmtpaddress) -ShowDetails -ResultSize 1 -StartDate ('2018-01-19T15:53:25Z') -EndDate ('2018-09-27T00:00:00Z')
}

"Finished checking mailbox searches on $serverName"



