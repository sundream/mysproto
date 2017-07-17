mysproto
========

A pure C# implementation of [sproto](https://github.com/cloudwu/sproto).

## Introduction
Sproto is an efficient serialization library. It's like Google protocol buffers.
The design is simple. It only supports a few types,such as integer/string/boolean/binary/group-buildin-type.

## Usage
```c#
//see Test/SimpleExample.cs
using System;
using System.Collections.Generic;
using Sproto;

namespace TestSproto {
	public static class SimpleExample {
		public static void Run () {
			string server_proto_str =
@"
.package {
	type 0 : integer
	session 1 : integer
}

.Person {
	id 0 : integer			# int type
	name 1 : string			# string type
	age 2 : integer			# int type
	isman 3 : boolean		# bool type
	emails 4 : *string		# string list
	children 5 : *Person	# Person list
	luckydays 6 : *integer  # integer list
}

get 1 {
	request {
		id 0 : integer
	}
	response Person
}
";
			string client_proto_str =
@"
.package {
	type 0 : integer
	session 1 : integer
}
";
			SprotoMgr client_proto = SprotoParser.Parse(client_proto_str);
			SprotoMgr server_proto = SprotoParser.Parse(server_proto_str);
			SprotoRpc client = client_proto.Attach(server_proto);
			SprotoRpc server = server_proto.Attach(client_proto);
			// create a request
			SprotoObject request = server_proto.NewSprotoObject("get.request");
			request["id"] = 1;
			RpcPackage request_package = client.Request("get",request,1);
			RpcInfo rpcinfo = server.Dispatch(request_package.data,request_package.size);
			// create a response
			SprotoObject response = server_proto.NewSprotoObject("Person");
			response["id"] = 1;
			response["name"] = "sundream";
			response["age"] = 26;
			response["emails"] = new List<string>{
				"linguanglianglgl@gmail.com",
			};
			//List<SprotoObject> children = new List<SprotoObject>();
			// no children
			//response["children"] = children;
			response["luckydays"] = new List<Int64>{0,6};
			RpcPackage response_package = server.Response("get",response,1);
			rpcinfo = client.Dispatch(response_package.data,response_package.size);
			Console.WriteLine("proto={0},tag={1},session={2},type={3},request={4},response={5}",
					rpcinfo.proto,rpcinfo.tag,rpcinfo.session,rpcinfo.type,rpcinfo.request,rpcinfo.response);

		}
	}
}
```

## C# API
* `SprotoMgr SprotoParser.Parse(string proto)` create a sprotomgr by proto string
* `SprotoMgr SprotoParser.ParseFile(string filename)` create a sprotomgr by proto file
* `SprotoMgr SprotoParser.ParseFile(List<string> filenames)` create a sprotomgr by proto file list
* `SprotoObject SprotoMgr.NewSprotoObject(string typename,object val=null)` create a SprotoObject by typename,we can set field like dict later
* `SprotoStream SprotoMgr.Encode(SprotoObject obj,SprotoStream writer=null)` encode a SprotoObject
* `SprotoObject SprotoMgr.Decode(string typename,SprotoStream reader)` decode to a SprotoObject
* `byte[] SprotoMgr.Pack(byte[] src,int start,int length,out int size,byte[] dest=null)` 0-pack a byte-buffer
* `byte[] SprotoMgr.Unpack(byte[] src,int start,int length,out int size,byte[] dest=null)` 0-unpack a byte-buffer
* `SprotoRpc SprotoMgr.Attach(SprotoMgr server)` create a SprotoRpc by a client sprotomgr attach a server sprotomgr
* `RpcPackage SprotoRpc.Request(string proto,SprotoObject request=null,Int64 session=0)` create a request package
* `RpcPackage SprotoRpc.Response(string proto,SprotoObject response=null,Int64 session=0)` create a response package
* `RpcInfo SprotoRpc.Dispatch(byte[] bytes,int size)` dispatch a request/response package

## Benchmark
In my i5-3210 @2.5GHz CPU, the benchmark is below:

|library         | encode 1M times | decode 1M times | size
|----------------| --------------- | --------------- | ----
|sproto(nopack)  | 9.193s          | 9.963s          | 130 bytes
|sproto          | 10.000s         | 10.411s         | 83 bytes

## See Also
* [sproto](https://github.com/cloudwu/sproto)
* [sproto-cs](https://github.com/jintiao/sproto-cs)
* [sproto-Csharp](https://github.com/lvzixun/sproto-Csharp)
* [sprotoparser](https://github.com/spin6lock/yapsp)
