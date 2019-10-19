# InjectGitVersion.ps1
#
# Set the version in the projects AssemblyInfo.cs file
#


# Get version info from Git. example 1.2.3-45-g6789abc
#$gitVersion = git describe --long --always;

# Parse Git version info into semantic pieces
#$gitVersion -match '(.*)-(\d+)-[g](\w+)$';
#$gitTag = $Matches[1];
#$gitCount = $Matches[2];
$gitSHA1 = git rev-list --max-count=1 HEAD;

# Define file variables
$assemblyFile = $args[0] + "\Properties\AssemblyInfo.cs";
$templateFile =  $args[0] + "\Properties\AssemblyInfo_template.cs";
$versionFile =  $args[0] + "\Properties\version.txt";

# Read template file, overwrite place holders with git version info
$newAssemblyContent = Get-Content $templateFile |
    #%{$_ -replace '\$FILEVERSION\$', ($gitTag + "." + $gitCount) } |
    %{$_ -replace '\$INFOVERSION\$', ($gitSHA1) };

$newVersionContent = Get-Content $versionFile;
$newVersionContent = $newVersionContent + "`r`n"  + ($gitSHA1);

# Write AssemblyInfo.cs file only if there are changes
If (-not (Test-Path $assemblyFile) -or ((Compare-Object (Get-Content $assemblyFile) $newAssemblyContent))) {
    echo "Injecting Git Version Info to AssemblyInfo.cs"
    $newAssemblyContent > $assemblyFile;
}

$sourceVersionFile =  $args[0] + "\Properties\git_hash.txt";
($gitSHA1) | Set-Content $sourceVersionFile