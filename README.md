# MMM Installer File
To make an MMM Installer File
first, create a folder with a file called `Info.json`
inside of Info.json there are 3 different options to do this allows for up to 3 different thing to install or more in zips
1.
```
{
  "dll": {modname}
}
```
replace `{modname}` with the name of your mod and the dll of your mod must be in the same folder as the `Info.json`
2.
```
{
  "zip": {mod/whatever name}
}
```
replace `{mod/whatever name}` with the name of what the thing that you are installing zips can contain anything really they will always go into a folder with the whatever you put where `{mod/whatever name}` is.
3.
```
{
  "download": {url to a download link}
}
```
replace `{url to a download link}` with a url to a download and this can download zips or dlls nothing else.

now after you have created a Info.json with whatever info
zip the folder up and rename it to {whatever}.mmm (note: requires show file extension to be enabled as you are changing .zip to .mmm)
once that's done test it and it should work!
Make sure to test it and check that in the zip/mmm file there isn't a folder cause if there is it won't work!
