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
There is at least one predefined space named `Default`. 

Space names are case-insensitive.

#### Sessions and Authorization
Accessing to any Space requires a valid session. There are two types of session: anonymous and real.
* *Anonymous* - just contain a Space name, isn't really opening. 
Anonymous session is valid to access spaces with no protection.
``` C#
var anonSession = ApiSession.Anonymous("some space");
```
* *Real session* - This session type is really created at server when user sent a valid credentials. 
It is automatically renewed each time when you're accessing the server with it.
Session is valid for a limited period of time and may be closed by inactivity or manually.


``` C#
//open session
var apiSession = await apiClient.OpenSessionAsync("space name", "password", cancellationToken);

try {
// do something...
}
finally {
//close session if it's no longer needed
await apiClient.CloseSessionAsync(apiSession,cancellationToken);
}
```

another way:
```C#
using(var apiSession = await apiClient.OpenSessionAsync("space name", "password", cancellationToken)){
// do somethig...

}
```
Session opening requires some handshake with server, so multiple requests are using.


Passing wrong credentials will throw `MorphApiUnauthorizedException`. 

#### Spaces
##### List of all spaces
This anonymous method returns entire list of all Spaces at server. 
``` C#
var spaces = await apiClient.GetSpacesListAsync(cancellationToken);
foreach(space in spaces){
///... 
}
```
##### Space status
Returns status of the Space with permissions.
``` C#
var apiSession = ApiSession.Anonymous("Default");
var spaceStaus = await apiClient.GetSpaceStatusAsync(apiSession, cancellationToken);
```

#### Tasks
Accessing to tasks require a valid api session.

Assume that you have already created the task in Space 'Default'. For these samples task id is `691ea42e-9e6b-438e-84d6-b743841c970e`.
Also assume, that you have read Sessions section and know how to open a session.

##### Tasks list

``` C#
  var apiSession = ApiSession.Anonymous("Default");

  var tasks = await client.GetTasksListAsync(apiSession, cancellationToken );
  foreach(var task in tasks){
  // do somethig with task
  }
```
If you want to get more details about task (e.g. task parameters) use `GetTaskAsync` method.

##### Starting the Task

To run the task:

``` C#
  var apiSession = ApiSession.Anonymous("Default");
  var taskGuid = Guid.Parse("691ea42e-9e6b-438e-84d6-b743841c970e");
  await client.StartTaskAsync(apiSession, taskGuid , cancellationToken );
```
Caller gets control back immediately after the task initialized to start. If task is already running no exception is generated.


##### Stopping the Task

To stop the task
``` C#
  var apiSession = ApiSession.Anonymous("Default");
  var taskGuid = Guid.Parse("691ea42e-9e6b-438e-84d6-b743841c970e");
  await client.StopTaskAsync(apiSession, "691ea42e-9e6b-438e-84d6-b743841c970e", cancellationToken )
```
Caller gets control back immediately after the task is marked to stop.

#### Retrieving task info
Allow you to get task info (incl info about task parameters)
``` C#
try {
    var apiSession = ApiSession.Anonymous("Default");
    var taskGuid = Guid.Parse("691ea42e-9e6b-438e-84d6-b743841c970e");
    var status = await client.GetTaskAsync(apiSession, taskGuid, cancellationToken );
  
    var task = await _apiClient.GetTaskAsync(apiSession, parameters.TaskId.Value, _cancellationTokenSource.Token);
    Console.WriteLine("Info about task:");
    Console.WriteLine(string.Format("Id:'{0}'", task.Id));
    Console.WriteLine(string.Format("Name:'{0}'", task.TaskName));
    Console.WriteLine(string.Format("IsRunning:'{0}'", task.IsRunning));                
    Console.WriteLine(string.Format("Enabled:'{0}'", task.Enabled));
    Console.WriteLine(string.Format("Note:'{0}'", task.Note));
    Console.WriteLine(string.Format("ProjectPath:'{0}'", task.ProjectPath));
    Console.WriteLine(string.Format("StatusText:'{0}'", task.StatusText));
    Console.WriteLine(string.Format("TaskState:'{0}'", task.TaskState));
    Console.WriteLine("Task Parameters:");
    foreach (var parameter in task.TaskParameters)
    {
        Console.WriteLine($"Parameter '{parameter.Name}' = '{parameter.Value}' [{parameter.ParameterType}] (Note: {parameter.Note})");
    }
    Console.WriteLine("Done");
 
}catch(MorphApiNotFoundException notFound){
  Console.WriteLine("Task not found");
}

```


#### Retrieving task status 

You can check task state (running/ not running) by calling `GetTaskStatusAsync`

``` C#
try {
  var apiSession = ApiSession.Anonymous("Default");
  var taskGuid = Guid.Parse("691ea42e-9e6b-438e-84d6-b743841c970e");
  var status = await client.GetTaskStatusAsync(apiSession, taskGuid, cancellationToken );
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
  var apiSession = ApiSession.Anonymous("Default");  
  var listing = await client.BrowseSpaceAsync(apiSession, "Folder 1/Folder 2",cancellationToken);
```


##### Upload file
For showing progress state changes while large files are uploading/downloading, you might subscribe to `FileProgress` event.

To upload file, call `UploadFileAsync`. It's consumes data directly from stream. There is also overloaded version, which takes file name.

``` C#
var apiSession = ApiSession.Anonymous("Default");  
await _apiClient.UploadFileAsync(apiSession, @"D:\data\file.xml", @"\folder 2", cancellationToken, overwriteFileifExists:false);
```

By default, this method WILL NOT overwrite file if it is already exists. In such case, exception will be raised.


Please consider that currently such kind of errors (file already exists, folder not found) are generated only AFTER entire request was sent to server. 

It will be a good practice to check if file/folder exists and you have appropriate permissions before sending huge files over a slow Internet connection. To so this, use `SpaceBrowseAsync`, `FileExistsAsync`.


##### Download file
For showing progress state changes while large files are uploading/downloading, you might subscribe to `FileProgress` event.
Download file:

``` C#
var apiSession = ApiSession.Anonymous("Default");  
using (Stream streamToWriteTo = File.Open(tempFile, FileMode.Create)) 
{
        let fileInfo  = await DownloadFileAsync(apiSession, @"\server\folder\file.xml", streamToWriteTo, cancellationToken);        
        Console.WriteLine("File " + fileInfo.FileName + "downloaded into " + tempFile);
}
                  
                    
```
##### Check the existence of the file

You can check that file exists by using `BrowseSpaceAsync`:
``` C#
  var apiSession = ApiSession.Anonymous("Default"); 
  var listing = await client.BrowseSpaceAsync(apiSession, "Folder 1/Folder 2",cancellationToken);
  // check that file somefile.txt exists in Folder 1/Folder 2
  if(listing.FileExists("somefile.txt"))
  {
     // do something...
  }
```



##### File deletion


``` C#
var apiSession = ApiSession.Anonymous("Default"); 
await DeleteFileAsync(apiSession, @"\server\folder", "file.xml", cancellationToken);

```



#### Commands

##### Task Validation
Now you can check tasks for missing parameters. 
E.g Task has parameters that Project doesn't contain. It is useful to call this method right after the Project has been uploaded to the server.

For now, there is no way to validate Project *before* upload.

``` C# 
var apiSession = ApiSession.Anonymous("Default"); 
ValidateTasksResult result = await ValidateTasksAsync(apiSession, @"folder 2\project.morph" , cancellationToken);
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




































