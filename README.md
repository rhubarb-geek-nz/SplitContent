# rhubarb-geek-nz/SplitContent
Split-Content tool for PowerShell

Reads the content of a file as either text or binary.

If as a text file then the output is split line by line.

If as a binary file then the output is a stream of byte arrays of a configured size.

```
Split-Content [-Path] <string[]> [-Encoding <Encoding>]

Split-Content [-Path] <string[]> -AsByteStream [-ReadCount <int>]

Split-Content -LiteralPath <string[]> [-Encoding <Encoding>]

Split-Content -LiteralPath <string[]> -AsByteStream [-ReadCount <int>]
```

See [test.ps1](test.ps1)
