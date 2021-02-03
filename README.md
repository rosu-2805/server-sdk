## EasyMorph Server .NET SDK

This library allows interacting with [EasyMorph Server](https://easymorph.com/server.html) from a .NET application.


EasyMorph Server runs on schedule projects created using desktop editions of EasyMorph (including the free edition). Project schedule and parameters are set up via task properties. Every task belongs to a space. The Server has at least one space named `Default`.


This document describes SDK which covers part of REST API v1 ( `http://[host:port]/api/v1/`). 


### EasyMorph Server API client
#### Supported platforms:
 * Windows (.NET framework version 4.5 or higher) 
 * Windows (.NET Standard 2.0 compatible framework required)
 * Linux   (.NET Standard 2.0 compatible framework required)

#### Download
The .NET SDK can be installed as a [NuGet package EasyMorph.Server.SDK](https://www.nuget.org/packages/EasyMorph.Server.SDK)

#### Introduction

To create an API client, you have to provide the server host to the constructor:
``` C#
  using Morph.Server.Sdk.Client;
  ...
  var apiUri = new Uri("http://192.168.1.1:6330");
  var client = new MorphServerApiClient(apiUri);
```

All commands are asynchronous.
Every command may raise an exception. You must take care to handle exceptions in the right way. EasyMorph Server SDK also has its own exceptions. They are described in the corresponding sections.

**Spaces**. EasyMorph Server may have one or several *spaces*. Each space has its own security restrictions. A space contains tasks and files.
There is at least one predefined space named `Default` that cannot be deleted.

Space names are case-insensitive.

#### Sessions and Authentication
Server Space can use one of the following authentication methods (user access restriction):
* Anonymous (No restriction)
* Space Password 
* Windows Authentication
  

Accessing any Space resource requires a valid ```ApiSession``` entity. Basically, there are two types of sessions: anonymous and real.
* *Anonymous* session just contains a Space name and isn't really opened. 
An anonymous session is used to access spaces with no protection.
* *Real session* is a session that is actually created in Server when a user sends valid credentials. 
It is automatically renewed each time when the session is used to interact with the Server.
A session is valid for a limited period of time and can be closed either manually or automatically due to inactivity.


```MorphServerApiClient``` is able to detect the required authentication method automatically. 

This sample shows how to construct a client, open a session, get data, close the session, and dispose the client:

``` C#
    OpenSessionRequest openSessionRequest = new OpenSessionRequest
    {
        SpaceName = "space name", // Space name, required.
        Password = password // optional property, required only when accessing a Password Protected Space.
    };
    
    var apiUri = new Uri("http://192.168.1.1:6330");
    
    using(var apiClient = new MorphServerApiClient(apiUri))
    using(var apiSession = await apiClient.OpenSessionAsync(openSessionRequest, CancellationToken.None)){
            // api session opened, get space tasks list
            var spaceTasks = await apiClient.GetTasksListAsync(apiSession, CancellationToken.None); 
        
    }

```
Opening a session requires handshaking with the Server. It is performed by a series of requests to the Server.

Passing wrong credentials will throw `MorphApiUnauthorizedException`.

To close a session, call ```apiSession.Dispose()``` or ```.CloseSessionAsync()``` method.
Don't forget to dispose ```MorphServerApiClient```, if it's no longer required.

To configure ```MorphServerApiClient``` use ```ClientConfiguration``` in ```MorphServerApiClient``` or  use ```MorphServerApiClientGlobalConfig``` for global configuration.

Pay attention, that setting ```ClientConfiguration.AutoDisposeClientOnSessionClose``` to true will cause ```apiClient``` disposal on session closing.   


###  Spaces API
##### List of all spaces
This method returns a list of all Server Spaces. 
``` C#
    var result = await apiClient.GetSpacesListAsync(cancellationToken);
    foreach(space in result.Items){
    ///... 
    }
```



##### Space status
Returns short info for the current Server space and current user permissions.
``` C#
  var spaceStaus = await apiClient.GetSpaceStatusAsync(apiSession, cancellationToken);
```

### Tasks API
Accessing tasks requires a valid ```apiSession```.

For the examples below, let's assume that you have already created a task in the 'Default' space, and the task ID is `691ea42e-9e6b-438e-84d6-b743841c970e`.
Also, let's assume that you have read the "Sessions and Authentication" section above and know how to open a session.

##### Tasks list

To get a list of tasks in Space, use ```GetTasksListAsync```. 

``` C#
  
  var result = await client.GetTasksListAsync(apiSession, cancellationToken );
  foreach(var task in result.Items){
  // do somethig with task
      if(task.TaskState == TaskState.Failed){
        // task failed
      }
  }
```
If you want to get more details about a task (e.g. task parameters), use the `GetTaskAsync` method.

##### Enabling/ disabling tasks

Use ```TaskChangeModeAsync```.

```  C#
     var taskGuid = Guid.Parse("691ea42e-9e6b-438e-84d6-b743841c970e");
     await apiClient.TaskChangeModeAsync(apiSession,taskGuid, 
        new TaskChangeModeRequest{TaskEnabled =false},
        cancellationToken);
```

##### Starting the Task

To run the task:

``` C#
  
    var taskGuid = Guid.Parse("691ea42e-9e6b-438e-84d6-b743841c970e");
    List<TaskParameterBase> taskParameters = new List<TaskParameterBase>
    {
        new TaskParameterBase{ Name ="String parameter", Value ="string parameter value" }
    };
    var result2 = await apiClient.StartTaskAsync(apiSession,
        new StartTaskRequest { TaskId = taskId, TaskParameters = taskParameters }, cancellationToken);
```
The caller gets control back immediately after the task is started. If you attempt to start a task that is already running, no exception is generated.


##### Stopping the Task

To stop the task, call ```StopTaskAsync```:
``` C#
  
    var taskGuid = Guid.Parse("691ea42e-9e6b-438e-84d6-b743841c970e");
    await client.StopTaskAsync(apiSession, taskGuid, cancellationToken );
```
The caller gets control back immediately after the task is instructed to stop.

#### Retrieving task info
Allows to get the task info (including the task parameters)
``` C#
    try {        
        var taskGuid = Guid.Parse("691ea42e-9e6b-438e-84d6-b743841c970e");
        var status = await client.GetTaskAsync(apiSession, taskGuid, cancellationToken );
          
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
     
    }
    catch(MorphApiNotFoundException notFound){
      Console.WriteLine("Task not found");
    }

```


#### Retrieving task status 

To check the task state (running/ not running / failed) and to retrieve task errors, call `GetTaskStatusAsync`:

``` C#
    try 
    {      
        var taskGuid = Guid.Parse("691ea42e-9e6b-438e-84d6-b743841c970e");
        var status = await client.GetTaskStatusAsync(apiSession, taskGuid, cancellationToken );
        if(status.IsRunning){
            Console.WriteLine(string.Format("Task {0} is running", status.TaskName));
        }
    }
    catch(MorphApiNotFoundException notFound){
        Console.WriteLine("Task not found");
    }

```


####

### Files API

The EasyMorph Server API allows accessing files of a Server space remotely.


##### Browsing files
To browse files and folders, use `SpaceBrowseAsync`.


``` C#
    public class SpaceBrowsingInfo
    {
        public ulong FreeSpaceBytes { get; set; }
        public string SpaceName { get; set; }
        
        public List<SpaceFolderInfo> Folders { get; set; }        
        public List<SpaceFileInfo> Files { get; set; }
        public List<SpaceNavigation> NavigationChain { get; set; }

       ...
    }
    public sealed class SpaceFileInfo
    {        
        public string Name { get; set; }        
        public string Extension { get; set; }        
        public long FileSizeBytes { get; set; }        
        public DateTime LastModified { get; set; }
    }

    
    public sealed class SpaceFolderInfo
    {       
        public string Name { get; set; }        
        public DateTime LastModified { get; set; }
    }

    public sealed class SpaceNavigation
    {
        public string Name { get; set; }
        public string Path { get; set; }
    }

    public async Task<SpaceBrowsingInfo> SpaceBrowseAsync(string spaceName, string folderPath, CancellationToken cancellationToken);


```

`Folder` and `Files` contain folder and file details in the `folderPath` of space `spaceName`.



Consider that there is "Folder 1" in space "Default". "Folder 1" has the nested "Folder 2".
To browse "Folder 2", you can call:

``` C#
   OpenSessionRequest openSessionRequest = new OpenSessionRequest
    {
        SpaceName = "Default", // Space name, required.
        Password = password // optional property, required only when accessing a Password Protected Space.
    };
    
    var apiUri = new Uri("http://192.168.1.1:6330");
    
    using(var apiClient = new MorphServerApiClient(apiUri))
    using(var apiSession = await apiClient.OpenSessionAsync(openSessionRequest, CancellationToken.None)){
        var listing = await apiClient.SpaceBrowseAsync(apiSession, "Folder 1/Folder 2",cancellationToken); 
        
    }
  
```


##### Upload stream

To upload a data stream, call `SpaceUploadDataStreamAsync`. 

``` C#

    /// <summary>
    /// Uploads specified data stream to the server space
    /// </summary>
    public sealed class SpaceUploadDataStreamRequest
    {
        /// <summary>
        /// Server folder path to place data file
        /// </summary>
        public string ServerFolder { get; set; }
        /// <summary>
        /// Stream to send to
        /// </summary>
        public Stream DataStream { get; set; }
        /// <summary>
        /// Destination server file name
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// File size. required for progress indication
        /// </summary>
        public long FileSize { get; set; }
        /// <summary>
        /// A flag to overwrite existing file. If flag is not set and file exists api will raise an exception
        /// </summary>
        public bool OverwriteExistingFile { get; set; } = false;
    }
  
    await apiClient.SpaceUploadDataStreamAsync(apiSession, 
        new SpaceUploadDataStreamRequest {
            ServerFolder =  @"\folder 2",
            DataStream = stream,
            FileName  = "file.dat",
            OverwriteExistingFile = false,
            FileSize = 9999 
        } ,     
      cancellationToken);
```


SpaceUploadContiniousStreamingAsync

Note that currently, such kind of errors (file already exists, folder not found) is generated only AFTER the entire request was sent to the Server. 

It will be a good idea to check if a file/folder exists and you have appropriate permissions before sending huge files over a slow internet connection. To do this, use `SpaceBrowseAsync`, `FileExistsAsync`.

To display progress changes while large files are uploaded, subscribe to the `OnDataUploadProgress` event.

   

##### Upload file

Use ```DataTransferUtility.SpaceUploadFileAsync``` :
```C# 
    var utility = new DataTransferUtility(apiClient,apiSession);
    await utility.SpaceUploadFileAsync(localFilePath,serverFolder,cancellationToken,overwriteExistingFile:false);

```

##### Download file stream
Use ```SpaceOpenDataStreamAsync``` to get a file content as a Stream.

```  C#
    using(var serverFileStream = await apiClient.SpaceOpenDataStreamAsync(apiSession,"some\file\path.xml", cancellationToken))
    {
        // read data from stream    
    }

```


To display progress changes while large files are downloading, subscribe to the `OnDataDownloadProgress` event.



##### Download file

Use ```DataTransferUtility.SpaceDownloadFileIntoFileAsync``` or ```DataTransferUtility.SpaceDownloadFileIntoFolderAsync``` :
```C# 
    var utility = new DataTransferUtility(apiClient,apiSession);
    await utility.SpaceDownloadFileIntoFolderAsync(remoteFilePath,targetLocalFolder,cancellationToken,overwriteExistingFile:false);

```


                  
                    

##### Check that file exists

You can check that a file exists by calling ```SpaceFileExistsAsync``` or by using `SpaceBrowseAsync`:
``` C#
 
    var listing = await apiClient.BrowseSpaceAsync(apiSession, "Folder 1/Folder 2",cancellationToken);
    // check that file somefile.txt exists in Folder 1/Folder 2
    if(listing.FileExists("somefile.txt"))
    {
     // do something...
    }
```



##### File deletion

To remove the file use ```SpaceDeleteFileAsync``` method:
``` C#

    await apiClient.SpaceDeleteFileAsync(apiSession, @"\server\folder\file.xml", cancellationToken);

```



### Commands API

##### Task validation
You can check tasks for missing parameters. 
E.g. a task has parameters that the project (used by the task) doesn't contain. It is useful to call this method right after the project has been uploaded to the Server.

For now, there is no way to validate a project *before* uploading.

``` C# 

ValidateTasksResult result = await apiClient.ValidateTasksAsync(apiSession, @"folder 2\project.morph" , cancellationToken);
if(result.FailedTasks.Count !=0 ){
     Console.WriteInfo("Some tasks have errors");
     foreach (var item in result.FailedTasks)
     {
        Console.WriteInfo(item.TaskId + ": " + item.Message + "@" + item.TaskApiUrl);
     }
}
```

### Exceptions
Morph.Server.SDK may raise its own exceptions like `MorphApiNotFoundException` if a resource is not found or `ParseResponseException` if it is not possible to parse Server response.
A full list of exceptions can be found in the `Morph.Server.Sdk.Exceptions` namespace.

### SSL
We recommend configuring EasyMorph Server to use SSL with a trusted SSL certificate. 

If you want to use a self-signed certificate, you need to handle this situation in your code. 

In such a case, you should setup a validation callback:  https://msdn.microsoft.com/en-us/library/system.net.servicepointmanager.servercertificatevalidationcallback.

One of the possible solutions can be found at the StackOverflow:  https://stackoverflow.com/a/526803/692329.

**Disclaimer:**  use any kind of self-signed certificates and security policy suppression at your own risk. We highly DO NOT RECOMMEND doing this.



## License 

**Morph.Server.SDK** is licensed under the [MIT license](https://github.com/easymorph/server-sdk/blob/master/LICENSE).
