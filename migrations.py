import json
import os
import shutil
import sys
from typing import Any, Callable

def migrateToV2(data: dict[str, Any]) -> tuple[dict[str, Any], str]:
    new_version: str = "v2"

    tracker_entries: dict[str, list[dict]] = data["TrackerEntries"]
    new_tracker_entries: dict[str, list[dict]] = dict()
    
    for date, entries in tracker_entries.items():
        new_entries: dict[str, dict] = dict()

        for entry in entries:
            if (not entry["Name"] in new_entries):
                new_entries[entry["Name"]] = {
                    "Name": entry["Name"],
                    "SubEntries": [
                        {
                            "Start": entry["Start"],
                            "End": entry["End"]
                        }
                    ]
                }

            else:
                new_entries[entry["Name"]]["SubEntries"] += [{"Start": entry["Start"], "End": entry["End"]}]

        new_tracker_entries[date] = [entry for entry in new_entries.values()]

    data["TrackerEntries"] = new_tracker_entries
    data["Verion"] = new_version

    return data, new_version

if __name__ == "__main__":
    migrations: dict[str, Callable[[dict[str, Any]], tuple[dict[str, Any], str]]] = {
        "v1": migrateToV2
    }

    data_directory_path: str = os.path.join(
        str(os.getenv("LOCALAPPDATA")),
        "Microsoft\\PowerToys\\PowerToys Run\\Settings\\Plugins\\Community.PowerToys.Run.Plugin.TimeTracker"
    )
    data_file_path: str = os.path.join(data_directory_path, "data.json")
    data_backup_file_path: str = os.path.join(data_directory_path, "data.json.backup")

    if (not os.path.isfile(data_file_path)):
        print("No data-file found. Migration aborted.\n")
        input("Press ENTER to continue...")
        sys.exit()

    if (os.path.isfile(data_backup_file_path)):
        os.remove(data_backup_file_path)
    shutil.copyfile(data_file_path, data_backup_file_path)

    with open(data_file_path, "r") as data_file:
        data: dict[str, Any] = json.loads(data_file.read())

    print(f"Data-file found and read. Current version: {data["Version"]}\nStarting migration...\n")

    while (True):
        if (data["Version"] not in migrations):
            print("No further migration needed.")
            break

        data, new_version = migrations[data["Version"]](data)
        print(f"Migrated to data version {new_version}.")

    with open(data_file_path, "w") as data_file:
        data_file.write(json.dumps(data))

    print("\nMigration done.\n")
    input("Press ENTER to continue...")