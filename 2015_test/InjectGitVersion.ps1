# InjectGitVersion.ps1
#
# Set the version in the projects AssemblyInfo.cs file
#


# Get version info from Git. example 1.2.3-45-g6789abc
$gitSHA1 = git log -1 --pretty=format:"%h";

$gitDate = git log -1 --pretty=format:"%cd";

$gitBranch = git rev-parse --abbrev-ref HEAD;

# Parse Git version info into semantic pieces
#$gitVersion -match '(.*)-(\d+)-[g](\w+)$';
#$gitTag = $Matches[1];
#$gitCount = $Matches[2];
#$gitSHA1 = $Matches[3];

# Define file variables
$assemblyFile = $args[0] + "\Properties\AssemblyInfo.cs";
$templateFile =  $args[0] + "\Properties\AssemblyInfo_template.cs";

# Read template file, overwrite place holders with git version info
$newAssemblyContent = Get-Content $templateFile |
    %{$_ -replace '\$DATE\$', ($gitDate) } |
    %{$_ -replace '\$COMMITHASH\$', ($gitSHA1) } |
    %{$_ -replace '\$BRANCH\$', ($gitBranch) };

# Write AssemblyInfo.cs file only if there are changes
If (-not (Test-Path $assemblyFile) -or ((Compare-Object (Get-Content $assemblyFile) $newAssemblyContent))) {
    echo "Injecting Git Version Info to AssemblyInfo.cs"
    $newAssemblyContent > $assemblyFile;       
}   