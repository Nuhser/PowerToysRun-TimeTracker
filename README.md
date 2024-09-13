# Time Tracker

## A plugin for Microsoft's PowerToys Run

### How to Install

1. Download the [latest release](https://github.com/Nuhser/PowerToysRun-TimeTracker/releases/latest) (e.g. `Release_1.x.x.zip`).
2. Unzip the archive. It should contain **one folder** named `TimeTracker`.
3. Close PowerToys.
4. Copy the folder into the plugin-folder of your PowerToys Run (e.g. `%LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins`).
5. Restart PowerToys Run.

You should now see **Time Tracker** inside your PowerToys Run Plugins. The default activation command is `+`.

### How to Update

1. Follow steps *1* to *4* from the installation guide.
2. Before restarting PowerToys, run the Python-script `migrations.py` from within the `TimeTracker`-directory.
   The script should handle the  complete migration of your data from its previous version to the current one.
3. Restart PowerToys.
