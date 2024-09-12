import json
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
    data_file_path: str = sys.argv[1]

    with open(data_file_path, "r") as data_file:
        data = json.loads(data_file.read())

    migration_complete: bool = False

    while (not migration_complete):
        match (data["Version"]):
            case "v1_0_0":
                data = migrateToV1_0_1(data)
                break
            case _:
                migration_complete = True
                break

    with open(data_file_path, "w") as data_file:
        data_file.write(json.dumps(data))

    input("Migration done")