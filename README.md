# FaithlifeUtils

### Summary

This is a quick and simple utility I wrote to export my notes from [Verbum](https://verbum.com/) or [Logos](https://www.logos.com/) and into markdown
for use in [Obsidian](https://obsidian.md/).  I've only tested this on the notes I've created so far, so I know there are likely major gaps in the
tool's note rich text parsing.

### Requirements

This code requires:
- [.NET 6](https://dotnet.microsoft.com/download) or higher and compatible development environment
- Any installed version of Verbum or Logos (the free edition is fine)

### Running

The code should be portable, though I've only tested this on my dev machine (Windows 10).
To run:
- Build via [Visual Studio](https://visualstudio.microsoft.com/vs/) or [JetBrain's Rider](https://www.jetbrains.com/rider/) or `dotnet build`
- Copy the binaries to a location of your choice
- Run the tool (see note below for LogosID/UserFolder)

```
--logosid         Required. The LogosId (from UserManager.db) for the user to run as

--userfolder      The UserFolder (UserId from UserManager.db) for the user

--outputfolder    Required. The output folder to write exports into

--notebookname    Required. The name of the notebook to export

--help            Display this help screen.

--version         Display version information.

Example:
PS C:\Users\me\git\FaithlifeUtils\FaithlifeUtils.exe --LogosId 12345 --OutputFolder "C:\Users\me\OneDrive\Obsidian\Religion" --NotebookName "Study Notes"
```

### To find your LogosID/UserFolder

- In Windows Explorer, open %LocalAppData% and navigate to your Verbum or Logos folder
- Using a tool like [DataGrip](https://www.jetbrains.com/datagrip/) or [DB Browser for SQLite](https://sqlitebrowser.org/) to open the Users/UserManager.db database
- Query the Users table to show you the LogosId
- In the Users table, you will also find your UserId (passed in as UserFolder to the util).
  - If there is only one user, UserFolder may be omitted

### Contact

If you'd find an issue or have a feature idea, feel free to [reach out](mailto:dadovan@live.com) to me.
