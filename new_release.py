from copy import deepcopy
import os.path
import json
import re
import shutil
import time
from zipfile import ZipFile

def get_version_tag():
  version_pattern = re.compile(r"v\d+\.\d+\.\d+\.\d+")
  while True:
    version_tag = input("Version Tag? (Example: v1.0.0.1): ")
    if version_pattern.match(version_tag):
      return version_tag
    print("Error: Must match vW.X.Y.Z where W, X, Y, and Z are integers")

def update_download_links(version_tag):
  json_path = os.path.join("FFLogsPartyLookup", "FFLogsPartyLookup.json")
  temp_path = os.path.join("FFLogsPartyLookup", "FFLogsPartyLookup.json.temp")

  with open(json_path, "r") as original:
    original_data = original.read()
  original_parsed = json.loads(original_data)
  was_list = False
  if isinstance(original_parsed, list):
    original_parsed = original_parsed[0]
    was_list = True
  new_parsed = deepcopy(original_parsed)
  new_link = f"https://github.com/SyntaxVoid/FFLogsPartyLookup/releases/download/{version_tag}/{version_tag}.zip"
  new_parsed["DownloadLinkInstall"] = new_link
  new_parsed["DownloadLinkTesting"] = new_link
  new_parsed["DownloadLinkUpdate"]  = new_link
  new_parsed["AssemblyVersion"] = version_tag[1:]
  new_parsed["LastUpdated"] = int(time.time())
  if was_list:
    new_data = json.dumps([new_parsed], indent=2)
  else:
    new_data = json.dumps(new_parsed, indent=2)
  with open(temp_path, "w") as new:
    new.write(new_data)
  os.unlink(json_path)
  os.rename(temp_path, json_path)
  return

def make_new_build(version_tag):
  version_number = version_tag[1:] # Strip the v off
  os.system(f"dotnet build /p:Version={version_number} /p:AssemblyVersion={version_number} /p:FileVersion={version_number}")
  return

def make_and_fill_new_release_folder(version_tag):
  
  ## Make Folder
  release_dir = "Releases"
  new_folder_name = os.path.join(release_dir, version_tag)
  try:
    os.mkdir(new_folder_name)
  except FileExistsError:
    print(f"A folder already exists at {new_folder_name}")
    return -1
  except Exception as e:
    print(f"Unknown error: {str(e)}")
    return -1
  
  ## Fill Folder
  # Sources
  dll_src  = os.path.join("FFLogsPartyLookup", "bin", "debug", "FFLogsPartyLookup.dll")
  json_src = os.path.join("FFLogsPartyLookup", "bin", "debug", "FFLogsPartyLookup.json")
  
  # Destinations
  dll_dst  = os.path.join(new_folder_name, "FFLogsPartyLookup.dll")
  json_dst = os.path.join(new_folder_name, "FFLogsPartyLookup.json")

  src_to_dst = { dll_src:  dll_dst,
                json_src: json_dst}

  for src, dst in src_to_dst.items():
    try:
      shutil.copy2(src, dst)
    except Exception as e:
      print(f"Unknown error: {str(e)}")
      return -1
  
  ## Zip Folder
  zip_dst = os.path.join(new_folder_name, f"{version_tag}.zip")
  with ZipFile(zip_dst, "w") as zip_object:
    try:
      for dst in src_to_dst.values():
        zip_object.write(dst, arcname=os.path.basename(dst))
    except Exception as e:
      print(f"Unknown error: {str(e)}")
      return -1
  
def push_to_github():
  while True:
    yn = input("Add, commit, and push to github? (Y/N): ").strip().lower()
    if yn in ("y", "n"):
      break
    print("wat")
  if yn == "y":
    os.system("git add .")
    commit_message = input("Commit message: ")
    os.system(f'git commit -m "{commit_message}"')
    os.system("git push")
    return
  if yn == "n":
    return

def cleanup_tasks_for_you(version_tag):
  print("Almost done!")
  print(f"Go to github and create a new release with the title: {version_tag}")
  print("Once that's done, upload the zipped file from: ")
  print(os.path.join("Releases", version_tag))
  print()
  print("Then go into Dalamud and make sure your update is recognized " \
      + "and that the new version is able to load, execute and unload.")
  return

if __name__ == "__main__":
  version_tag = get_version_tag()
  print("Updating download links...")
  time.sleep(1.5)
  update_download_links(version_tag)
  print("  Download links updated!")
  time.sleep(0.5)
  print("Making new build...")
  make_new_build(version_tag)
  print("  New build made!")
  if make_and_fill_new_release_folder(version_tag) == -1:
    print("Unable to make release. See errors above.")
    exit()
  push_to_github()
  cleanup_tasks_for_you(version_tag)


  