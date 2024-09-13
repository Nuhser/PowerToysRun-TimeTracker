import json
import os
import shutil
import sys

def migrateToV1_0_1(data):
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
    data["Verion"] = "v1_0_1"
    return data

if __name__ == "__main__":
    data_directory_path: str = os.path.join(
        str(os.getenv("LOCALAPPDATA")),
        "Microsoft\\PowerToys\\PowerToys Run\\Settings\\Plugins\\Community.PowerToys.Run.Plugin.TimeTracker"
    )
    data_file_path: str = os.path.join(data_directory_path, "data.json")
    data_backup_file_path: str = os.path.join(data_directory_path, "data.json.backup")

    print(data_file_path)

    if (not os.path.isfile(data_file_path)):
        print("No data-file found. Migration aborted.\n")
        input("Press ENTER to continue...")
        sys.exit()

    if (os.path.isfile(data_backup_file_path)):
        os.remove(data_backup_file_path)
    shutil.copyfile(data_file_path, data_backup_file_path)

    with open(data_file_path, "r") as data_file:
        data = json.loads(data_file.read())

    print(f"Data-file found and read. Current version: {data["Version"]}\nStarting migration...\n")

    migration_complete: bool = False

    while (not migration_complete):
        match (data["Version"]):
            case "v1_0_0":
                print(f"Migrating to version 1.0.1...")
                data = migrateToV1_0_1(data)
                break
            case _:
                print("No further migration needed.")
                migration_complete = True
                break

    with open(data_file_path, "w") as data_file:
        data_file.write(json.dumps(data))

    print("\nMigration done.\n")
    input("Press ENTER to continue...")