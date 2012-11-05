Project ERA Server : ResourceService
===================================
This is the ResourceServer repository, which provides EraS and
several services in sub projects. All the services rely on EraS for
communication, but run as seperate processes whatsoever.

Public Function
---------------
	
	GetFile
	
	GetChunks
	
	GetChunk


Local Functions
--------------

_I will edit this portion as soon as anprotocol is available, 
but for now I will list all the capabilities._

	Asset.SaveFile( filenName[, previousName] ) 
	// saves a file to the server and returns file information
  
will save the file at the filenName path. If previousName is given
it will be queried from the server and retrieved and compared with
the file at the filenName path. If needed the retrieved file will
be updated. This function automatically generates an ingame
FileName format.

Once you have saved the asset, the calling object has all the
properties of the asset. So if you want to find out the FileName,
just ask it after saving the file by looking at the FileName 
property of the asset.

	static Asset.GetFile( fileName )
	// gets a file information from inagme filename
  
will get the file information from the server according to the ingame
fileName specified. This file information asset file is the same as
when you would save a file to the server, so the following functions
become available.

	// requires file information
	static Asset.GetChunksById( assetId, out chunksize, out length )
	// gets a list of chunk id's
	
will return a list of chunk object id's that compose this file in the
database. When you have the file information, you have the database id
and with that you can query all the chunk id's. The chunks together 
make the complete file.

	// requires file information
	static Asset.GetChunkById( chunkId )
	// gets the chunk as binarydata
	
returns the chunk's data. You can get the chunk id by requesting all
the id's for a file with GetChunksById. A file is composed by appending
all these binary data blocks in the right order.
	
	// requires file information
	Asset.Download ( fileName )
	// downloads a file
  
will download the asset to filenName path, if it is found on the
server. Use this to retrieve a file from the database.