## EasyMorph Sever SDK

This library allows you to interact with EasyMorph Server.


**EasyMorph Server** runs on schedule projects created using desktop editions of EasyMorph (including the **free edition**). Project schedule and parameters are setted up via Task properties. Every Task is placed into Space. Currently there is only one Space, which is named `Default`.


This document describes SDK which cover part of REST API v1 ( `http://[host:port]/api/v1/`). 


### EasyMorph Server API client
#### Depencencies:
* .NET framework version 4.5 or higher
* That's all

#### Download
Feel free to download it from [ nuget EasyMorph.Server.SDK](https://www.nuget.org/packages/EasyMorph.Server.SDK)

#### Introduction

To create Api client, you have to pass server host 
``` C#
  using Morph.Server.Sdk.Client;
  ...
  var client = new MorphServerApiClient("http://192.168.1.1:6330");
```

All commands are async.
Every command  may raise an exception. You must take care to handle exceptions in a right way. EasyMorph Server SDK also has own exceptions, they are described in corresponding section.

**Spaces**. Workspace is splitted into several Spaces. Each space has it's own security restrictions. Space contains Tasks and Files.
There is at least one predefined space named `Default`. It's a public. 

Space names are case-insensitive.




#### Tasks
Assume that you have already created the task in Space 'Default'. For these samples task id is `691ea42e-9e6b-438e-84d6-b743841c970e`.

##### Starting the Task

To run the task:

``` C#
  await client.StartTaskAsync("Default", "691ea42e-9e6b-438e-84d6-b743841c970e", cancellationToken );
```
Caller gets control back immediately after the task initialized to start. If task is already running no exception is generated.


##### Stopping the Task

To stop the task
``` C#
  await client.StopTaskAsync("Default", "691ea42e-9e6b-438e-84d6-b743841c970e", cancellationToken )
```
Caller gets control back immediately after the task is marked to stop.

#### Retrieving task status 

You can check task state (running/ not running) by calling `GetTaskStatusAsync`

``` C#
try {
  var status = await client.GetTaskStatusAsync("691ea42e-9e6b-438e-84d6-b743841c970e", cancellationToken );
  if(status.IsRunning){
    Console.WriteLine(string.Format("Task {0} is running", status.TaskName));
  }
}catch(MorphApiNotFoundException notFound){
  Console.WriteLine("Task not found");
}

```

#### Files API

EasyMorph Server allows you to access Space files remotely. 


##### Browsing files
To browse files and folders use `BrowseSpaceAsync`.


``` C#
public sealed class SpaceBrowsingInfo
  {
        public ulong FreeSpaceBytes { get; set; }
        public string SpaceName { get; set; }
        public WebFilesAccesMode WebFilesAccesMode { get; set; }

        public List<SpaceFolderInfo> Folders { get; set; }        
        public List<SpaceFileInfo> Files { get; set; }
        public List<SpaceNavigation> NavigationChain { get; set; }
        
        ...
  }

public async Task<SpaceBrowsingInfo> BrowseSpaceAsync(string spaceName, string folderPath, CancellationToken cancellationToken);


```

* `Folder` and `Files` contains content for requested `folderPath` in Space `spaceName`.
* `WebFilesAccesMode` shows restrictions for files in Space. You can check permissions before trying to upload or download files.



Consider, that there is Folder 1 in space Default. *Folder 1* has nested Folder 2.
So to browse Folder 2 you can call:

``` C#
  var listing = await client.BrowseSpaceAsync("Default", "Folder 1/Folder 2",cancellationToken);
```


##### Upload file
For showing progress state changes while large files are uploading/downloading, you might subscribe to `FileProgress` event.

To upload file, call `UploadFileAsync`. It's consumes data directly from stream. There is also overloaded version, which takes file name.

``` C#
await _apiClient.UploadFileAsync("Default", @"D:\data\file.xml", @"\folder 2", cancellationToken, overwriteFileifExists:false);
```

By default, this method WILL NOT overwrite file if it is already exists. In such case, exception will be raised.


Please consider that currently such kind of errors (file already exists, folder not found) are generated only AFTER entire request was sent to server. 

It will be a good practice to check if file/folder exists and you have appropriate permissions before sending huge files over a slow Internet connection. To so this, use `SpaceBrowseAsync`, `FileExistsAsync`.


##### Download file
For showing progress state changes while large files are uploading/downloading, you might subscribe to `FileProgress` event.
Download file:

``` C#
using (Stream streamToWriteTo = File.Open(tempFile, FileMode.Create)) 
{
        let fileInfo  = await DownloadFileAsync("Default", @"\server\folder\file.xml", streamToWriteTo, cancellationToken);        
        Console.WriteLine("File " + fileInfo.FileName + "downloaded into " + tempFile);
}
                  
                    
```
##### Check the existence of the file

You can check that file exists by using `BrowseSpaceAsync`:
``` C#
  var listing = await client.BrowseSpaceAsync("Default", "Folder 1/Folder 2",cancellationToken);
  // check that file somefile.txt exists in Folder 1/Folder 2
  if(listing.FileExists("somefile.txt"))
  {
     // do something...
  }
```



##### File deletion


``` C#
await DeleteFileAsync("Default", @"\server\folder", "file.xml", cancellationToken);

```



#### Commands

##### Task Validation
Now you can check tasks for missing parameters. 
E.g Task has parameters that Project doesn't contain. It is useful to call this method right after the Project has been uploaded to the server.

For now, there is no way to validate Project *before* upload.

``` C# 
ValidateTasksResult result = await ValidateTasksAsync("Default", @"folder 2\project.morph" , cancellationToken);
if(result.FailedTasks.Count !=0 ){
     Console.WriteInfo("Some tasks have errors");
     foreach (var item in result.FailedTasks)
     {
        Console.WriteInfo(item.TaskId + ": " + item.Message + "@" + item.TaskApiUrl);
     }
}
```

### Exceptions
Morph.Server.SDK may raise own exceptions like `MorphApiNotFoundException` if resource not found, or `ParseResponseException` if it is not possible to parse server response.
Full list of exceptions can be found in `Morph.Server.Sdk.Exceptions` namespace.


## License 

**Morph.Server.SDK** is licensed under the [MIT license](https://github.com/easymorph/server-sdk/blob/master/LICENSE).




































