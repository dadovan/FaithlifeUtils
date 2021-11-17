# FaithlifeUtils

### Summary

This is a quick and simple utility I wrote to export my notes from [Verbum](https://verbum.com/) or [Logos](https://www.logos.com/) and into markdown
for use in [Obsidian](https://obsidian.md/).  I've only tested this on the notes I've created so far so I know there are likely major gaps in the
tool's note rich text parsing.


### Running

The code should be portable, though I've only tested this on my dev machine (Windows 10).
To run:
- Build via [Visual Studio](https://visualstudio.microsoft.com/vs/) or [JetBrain's Rider](https://www.jetbrains.com/rider/)
- Copy the binaries to a location of your choice
- Update the [config.json](/FaithlifeUtils/config.json) file with your information (see [Configuration](#configuration) below)
- Run and enjoy!

### Configuration

To update your [config.json](/FaithlifeUtils/config.json):
- In Windows Explorer, open %LocalAppData% and navigate to your Verbum or Logos folder
- Using a tool like [DataGrip](https://www.jetbrains.com/datagrip/) or [DB Browser for SQLite](https://sqlitebrowser.org/) to open the Users/UserManager.db database
- Query the Users table to show you the LogosId and UserId (called UserFolder in the config.json file)
- Modify the OutputFolder to be wherever you'd like
- Set NotebookName is the name of the notebook you'd like to export from Verbum/Logos

### Contact

If you'd find an issue or have a feature idea, feel free to [reach out](dadovan@live.com) to me.
