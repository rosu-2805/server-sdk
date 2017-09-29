## EasyMorph Sever SDK

This library allows you to interact with EasyMorph Server.


**EasyMorph Server** runs on schedule projects created using desktop editions of EasyMorph (including the **free edition**). Project schedule and parameters are setted up via Task properties. Every Task is placed into Space. Currently there is only one Space, which is named `Default`.


This document describes SDK which cover part of REST API v1 ( `http://[host:port]/api/v1/`). 




### EasyMorph Server API client
#### Depencencies:
* .NET framework version 4.5 or higher
* That's all

#### Introduction

To create Api client, you have to pass server host 
``` C#
  using Morph.Server.Sdk.Client;
  ...
  var client = new MorphServerApiClient("http://192.168.1.1:6330");
```

All commands are async.
Every command  may raise an exception. You must take care to handle exceptions in a right way. EasyMorph Server SDK also has own exceptions, they are described in corresponding section.

**Spaces**. System workspace splited into several Spaces. Each space has it's own security restrictions. Space contains Tasks and Files.
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
``` C#
 public async Task StopTaskAsync(string spaceName, Guid taskId, CancellationToken cancellationToken)
```

To stop the task
``` C#
  await client.StopTaskAsync("Default", "691ea42e-9e6b-438e-84d6-b743841c970e", cancellationToken )
```
Caller gets control back immediately after the task is marked to stop.

#### Task status

You can check task status (running/ not runnig) by calling GetRunningTaskStatusAsync method

``` C#
try {
  var status = await client.GetRunningTaskStatusAsync("Default", "691ea42e-9e6b-438e-84d6-b743841c970e", cancellationToken );
  if(status.IsRunning){
    Console.WriteLine(string.Format("Project {0} is running", status.ProjectName));
  }
}catch(MorphApiNotFoundException notFound){
  Console.WriteLine("Task not found");
}

```

#### Files API

EasyMorph Server allows you to access to Space files remotely. 


##### Browsing files
To browse files and folders in Space, call BrowseSpaceAsync.


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
* `WebFilesAccesMode` shows restrictions for files in the Space. You can check it before trying to upload or download files.



Consider, that there is Folder 1 in space Default. *Folder 1* has nested Folder 2.
So to browse Folder 2 you can call:

``` C#
  var listing = await client.BrowseSpaceAsync("Default", "Folder 1/Folder 2",cancellationToken);
```


##### Files upload
For showing progress state changes while large files are uploading/downloading, you might subscribe to `FileProgress` event.

To upload file, call `UploadFileAsync`. It's consumes data directly from stream. There is also overloaded version, which takes file name.

```
await _apiClient.UploadFileAsync("Default", @"D:\data\file.xml", @"\folder 2", cancellationToken, overwriteFileifExists:false);
```

By default, this method WILL NOT overwrite file if it is already exists. In such case, exception will be raised.


Please consider that currently such kind of errors (file already exists, folder not found) are generated only AFTER entire request was sent to server. 

It will be a good practice to check if file/folder exists and you have appropriate permissions before sending huge files over a slow intertet connection. To so this, use `SpaceBrowseAsync`, `FileExistsAsync`.


##### Files download
For showing progress state changes while large files are uploading/downloading, you might subscribe to `FileProgress` event.





















#### Starting the Task
To strart the task, call 
POST runningtasks/{taskid}



```C#


```













