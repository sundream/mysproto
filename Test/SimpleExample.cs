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
