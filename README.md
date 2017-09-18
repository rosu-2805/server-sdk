## EasyMorph Sever SDK

This library allows you to interact with EasyMorph Server.
EasyMorph Server runs on schedule projects created using desktop editions of EasyMorph (including the free edition). Project schedule and parameters are setted up via Task properties. Every Task is placed into Space. Currently there is only one Space, which is named 'Default'.



This document describes API v1. 
Api url for version 1 is http://[host:port]/api/v1/
Most commands are REST compilant, except some cases. 


### EasyMorph Server API client
#### Depencencies:
* .NET framework version 4.5 or higher
* That's all

#### Initialization

To create Api client, you have to pass url to EasyMorph Server. 
``` C#
  using Morph.Server.Sdk.Client;
  ...
  var client = new MorphServerApiClient("http://192.168.1.1:6330");
```

All commands are async.
Every command  may raise an exception. You must take care to handle exceptions in a right way. EasyMorph Server SDK also has own exceptions, they are described in corresponding section.


#### Tasks
Assume that you have already created the task in Space 'Default'. For these samples task id is 691ea42e-9e6b-438e-84d6-b743841c970e.

##### Starting the Task

To run the task, just call:

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
Caller gets control back immediately after the task is marked to stop. If task is not running no exception is generated.

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
##### Browsing files

``` C#
public async Task<SpaceBrowsingInfo> BrowseSpaceAsync(string spaceName, string folderPath, CancellationToken cancellationToken);

public sealed class SpaceBrowsingInfo
  {
      public ulong FreeSpaceBytes { get; set; }
      public string SpaceName { get; set; }

      public List<SpaceFolderInfo> Folders { get; set; }        
      public List<SpaceFileInfo> Files { get; set; }
      public List<SpaceNavigation> NavigationChain { get; set; }

      public SpaceBrowsingInfo()
      {
          Folders = new List<SpaceFolderInfo>();
          Files = new List<SpaceFileInfo>();
          NavigationChain = new List<SpaceNavigation>();
      }
  }

```


To browse files and directories in Space, call BrowseSpaceAsync.
Consider, that there is Folder 1 in space Default. *Folder 1* has nested Folder 2.
So to browse Folder 2 you can call:

``` C#
  var listing = await client.BrowseSpaceAsync("Default", "Folder 1/Folder 2");
```

Or 
``` C#
  var listing = await client.BrowseSpaceAsync("Default", "Folder 1\Folder 2");
```
















#### Starting the Task
To strart the task, call 
POST runningtasks/{taskid}



```C#


```













