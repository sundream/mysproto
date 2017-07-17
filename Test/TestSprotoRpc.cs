using System;
using Sproto;

namespace TestSproto {
	public static class TestSprotoRpc {
		public static void Run () {
			Console.WriteLine("TestSprotoRpc.Run ...");
			string server_proto_str =
@".package {
	type 0 : integer
	session 1 : integer
}

foobar 1 {
	request {
		what 0 : string
	}
	response {
		ok 0 : boolean
	}
}

foo 2 {
	response {
		ok 0 : boolean
	}
}

bar 3 {
	response nil
}

blackhole 4 {
}
";

			string client_proto_str =
@".package {
	type 0 : integer
	session 1 : integer
}
";
			SprotoMgr client_proto = SprotoParser.Parse(client_proto_str);
			SprotoMgr server_proto = SprotoParser.Parse(server_proto_str);
			SprotoRpc client = client_proto.Attach(server_proto);
			// SprotoRpc client = new SprotoRpc(client_sproto,server_sproto);
			SprotoRpc server = server_proto.Attach(client_proto);
			// SprotoRpc server = new SprotoRpc(server_proto,client_proto);
			// test proto foobar
			SprotoObject request = server_proto.NewSprotoObject("foobar.request");
			request["what"] = "foo";
			RpcPackage request_package = client.Request("foobar",request,1);
			Console.WriteLine("client request foobar: data={0},size={1}",request_package.data,request_package.size);
			RpcInfo rpcinfo = server.Dispatch(request_package.data,request_package.size);

			SprotoHelper.Assert(rpcinfo.proto == "foobar");
			SprotoHelper.Assert(rpcinfo.type == "request","not a request");
			SprotoHelper.Assert(rpcinfo.request != null);
			SprotoHelper.Assert(rpcinfo.request["what"] == "foo");
			SprotoHelper.Assert(rpcinfo.session == 1);
			SprotoObject response = server_proto.NewSprotoObject("foobar.response");
			response["ok"] = true;
			RpcPackage response_package = server.Response(rpcinfo.proto,response,rpcinfo.session);
			Console.WriteLine("server resonse foobar: data={0},size={1}",response_package.data,response_package.size);
			rpcinfo = client.Dispatch(response_package.data,response_package.size);
			SprotoHelper.Assert(rpcinfo.proto == "foobar");
			SprotoHelper.Assert(rpcinfo.type == "response","not a response");
			SprotoHelper.Assert(rpcinfo.response != null);
			SprotoHelper.Assert(rpcinfo.response["ok"] == true);
			SprotoHelper.Assert(rpcinfo.session == 1);

			// test proto foo
			request_package = client.Request("foo",null,2);
			Console.WriteLine("client request foo: data={0},size={1}",request_package.data,request_package.size);
			rpcinfo = server.Dispatch(request_package.data,request_package.size);

			SprotoHelper.Assert(rpcinfo.proto == "foo");
			SprotoHelper.Assert(rpcinfo.type == "request","not a request");
			SprotoHelper.Assert(rpcinfo.request == null); // no request data
			SprotoHelper.Assert(rpcinfo.session == 2);
			response = server_proto.NewSprotoObject("foo.response");
			response["ok"] = false;
			response_package = server.Response(rpcinfo.proto,response,rpcinfo.session);
			Console.WriteLine("server resonse foo: data={0},size={1}",response_package.data,response_package.size);
			rpcinfo = client.Dispatch(response_package.data,response_package.size);
			SprotoHelper.Assert(rpcinfo.proto == "foo");
			SprotoHelper.Assert(rpcinfo.type == "response","not a response");
			SprotoHelper.Assert(rpcinfo.response != null);
			SprotoHelper.Assert(rpcinfo.response["ok"] == false);
			SprotoHelper.Assert(rpcinfo.session == 2);

			// test proto bar
			request_package = client.Request("bar",null,3);
			Console.WriteLine("client request bar: data={0},size={1}",request_package.data,request_package.size);
			rpcinfo = server.Dispatch(request_package.data,request_package.size);

			SprotoHelper.Assert(rpcinfo.proto == "bar");
			SprotoHelper.Assert(rpcinfo.type == "request","not a request");
			SprotoHelper.Assert(rpcinfo.request == null); // no request data
			SprotoHelper.Assert(rpcinfo.session == 3);
			response_package = server.Response(rpcinfo.proto,null,rpcinfo.session);
			Console.WriteLine("server resonse bar: data={0},size={1}",response_package.data,response_package.size);
			rpcinfo = client.Dispatch(response_package.data,response_package.size);
			SprotoHelper.Assert(rpcinfo.proto == "bar");
			SprotoHelper.Assert(rpcinfo.type == "response","not a response");
			SprotoHelper.Assert(rpcinfo.response == null); // no response data
			SprotoHelper.Assert(rpcinfo.session == 3);
		
			// test proto blackhole
			request_package = client.Request("blackhole",null,0);
			Console.WriteLine("client request blackhole: data={0},size={1}",request_package.data,request_package.size);
			rpcinfo = server.Dispatch(request_package.data,request_package.size);

			SprotoHelper.Assert(rpcinfo.proto == "blackhole");
			SprotoHelper.Assert(rpcinfo.type == "request","not a request");
			SprotoHelper.Assert(rpcinfo.request == null); // no request data
			SprotoHelper.Assert(rpcinfo.session == 0);
			// session == 0 mean: can donn't response

			server_proto.Dump();
			Console.WriteLine("TestSprotoRpc.Run ok");
		}
	}
}

