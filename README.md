# 🌥 Google Drive X Hard Disk Synchronizer 💾

This is a tool that ensures your favorite files on Google Drive are also available on your local hard disk. Useful for backing up important files or having offline access to your favorite media. Say goodbye to manual downloads and enjoy automated file synchronization.

## ⚙ How it Works

Once launched, this tool will scan the files in your configured Google Drive and local hard disk directories. It compares the file list and starts downloading the missing files from the Drive to the hard disk. The downloading is smart! It respects your daily data usage and ensures a safe margin is always left.

## 🔧 Configuration

All the magic numbers are in the `appsettings.json` file:

- `Folders:GoogleDriveFolder` & `Folders:HardDiskFolder`: Paths to your Google Drive and hard disk directories.
- `LoginDetails:Username`, `LoginDetails:Password` & `LoginDetails:SubscriberID`: Your SLT login details.
- `DataReserve`: Amount of data (in GB) to keep untouched.

Update these according to your preferences and you're good to go.

## ⚡ Quick Start

1. Clone this repository.
2. Navigate to the cloned directory.
3. Run `dotnet restore` to install dependencies.
4. Update `appsettings.json` with your own preferences.
5. Run the project.

Happy synchronizing!
