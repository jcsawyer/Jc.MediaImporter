# Jc.MediaImporter

![](img/Jc.MediaImporter.png)

A simple GUI application that allows me to conveniently import media (photos/videos) captured from various devices in the family.

## About

You can find some more information on [my blog post](https://jcsawyer.com/blog/2024/04/13/modernizing-my-media-importer/).

A while back after starting my family, it became quickly apparent there was no real convenient way to manage photos and videos captured across all our devices and put them on server in the correct shape without requiring a week to go through everything.

I created an initial version in .NET Core Windows Forms but this no longer works now I'm daily driving a Mac. So `Jc.MediaImporter` was born.

## Features
- Import photos and videos from a source directory to a target directory with accurate date-based naming conventions
- Import page to select photos and videos to import
- Duplicate detection in import process (where a photo already exists in the target directory)
- Settings page to configure source and target directories
- Identify existing duplicates in the target directory

## How it Works

You set your source directory, photo, and video target directories in the settings page and run an import, selecting the photos you wish to import.

When importing, photos and videos are moved and renamed according to the following structure:
```
/ target directory
-- / YYYY-MM
---- / YYYYMMDD_HH:mm:ss.{extension}
-- / YYYY-MM
---- / YYYYMMDD_HH:mm:ss.{extension}
```

## TODO

This was just thrown together to get it up and running now I'm on a Mac, but I wanted to share the source as it's not anything particularly big or powerful but may help someone. This does however mean it's currently limited to my specific use case.

- Make the naming conventions configurable
- ~Make the UI a little nicer on the eyes~
- Maybe handle more than just photos and videos
- Better duplicate handling in the import process
- Delete duplicates from the source directory
- Manage process to enable complete management of duplicates
- Implement import cancellation
- Reduce memory consumption
