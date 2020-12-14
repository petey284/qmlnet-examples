$pwd = "$(((pwd).Path -as [System.IO.DirectoryInfo]).Name)\\"
(ls -Dir -Exclude bin,obj) |
    Foreach { (ls $_).fullname } |
    Foreach { ($_ -Split $pwd)[1] } | Out-File -FilePath .\resources.txt -Encoding utf8
"Finshed saving resource list."