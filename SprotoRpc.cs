using System;
using System.Collections.Generic;

namespace Sproto {
	public class RpcPackage {
		public byte[] data = null;
		public int size = 0;
	}

	public class RpcInfo {
		public Int64 session = 0;
		public string proto = null;	// proto name
		public UInt16 tag = 0;		// proto tag
		public SprotoObject request = null;
		public SprotoObject response = null;
		public string type = null; //request or response
	}

	public class SprotoRpc {
		private SprotoMgr client; // self-side
		private SprotoMgr server; // server-side
		private SprotoStream writer;
		private SprotoStream reader;
		private Dictionary<Int64,RpcInfo> sessions;

		public SprotoRpc (SprotoMgr client,SprotoMgr server) {
			this.client = client;
			this.server = server;
			this.sessions = new Dictionary<Int64,RpcInfo>();
			this.writer = new SprotoStream();
			this.reader = new SprotoStream();
		}

		public RpcPackage Request(string proto,SprotoObject request=null,Int64 session=0) {
			//Console.WriteLine("request {0} {1} {2}",proto,request,session);
			SprotoProtocol protocol = this.server.GetProtocol(proto);
			UInt16 tag = protocol.tag;
			SprotoObject header = this.NewPackageHeader(this.server,tag,session);
			this.writer.Seek(0,SprotoStream.SEEK_BEGIN); // clear stream
			SprotoStream writer = this.server.Encode(header,this.writer);
			if (request != null) {
				string expect = protocol.request;
				if (request.type != expect)
					SprotoHelper.Error("[SprotoRpc.Request] expect '{0}' got '{1}'",expect,request.type);
				writer = this.server.Encode(request,writer);
			}
			RpcPackage package = new RpcPackage();
			package.data = this.server.Pack(writer.Buffer,0,writer.Position,out package.size);
			if (session != 0) {
				SprotoHelper.Assert(!this.sessions.ContainsKey(session),String.Format("repeat session: {0}",session));
				RpcInfo rpcinfo = new RpcInfo();
				rpcinfo.session = session;
				rpcinfo.proto = proto;
				rpcinfo.request = request;
				rpcinfo.tag = tag;
				this.sessions[session] = rpcinfo;
			}
			return package;
		}

		public RpcPackage Response(string proto,SprotoObject response=null,Int64 session=0) {
			//Console.WriteLine("response {0} {1} {2}",proto,response,session);
			SprotoProtocol protocol = this.client.GetProtocol(proto);
			SprotoObject header = this.NewPackageHeader(this.client,0,session);
			this.writer.Seek(0,SprotoStream.SEEK_BEGIN); // clear stream
			SprotoStream writer = this.client.Encode(header,this.writer);
			if (response != null) {
				string expect = protocol.response;
				if (response.type != expect)
					SprotoHelper.Error("[SprotoRpc.Response] expect '{0}' got '{1}'",expect,response.type);
				writer = this.client.Encode(response,writer);
			}
			RpcPackage package = new RpcPackage();
			package.data = this.client.Pack(writer.Buffer,0,writer.Position,out package.size);
			return package;
		}

		public RpcInfo Dispatch(byte[] bytes,int size) {
			RpcInfo rpcinfo = null;
			int bin_size = 0;
			byte[] bin = this.client.Unpack(bytes,0,size,out bin_size);
			this.reader.Seek(0,SprotoStream.SEEK_BEGIN); // clear stream
			this.reader.Buffer = bin;

			SprotoObject header = this.client.Decode("package",this.reader);
			if (header["type"] != null) {
				// request
				UInt16 tag = (UInt16)header["type"];
				SprotoProtocol protocol = this.client.GetProtocol(tag);
				SprotoObject request = null;
				if (protocol.request != null) {
					request = this.client.Decode(protocol.request,this.reader);
				}

				rpcinfo = new RpcInfo();
				rpcinfo.type = "request";
				if (header["session"] != null)
					rpcinfo.session = header["session"];
				rpcinfo.proto = protocol.name;
				rpcinfo.tag = protocol.tag;

				rpcinfo.request = request;
			} else {
				// response
				SprotoHelper.Assert(header["session"] != null,"session not found");
				Int64 session = header["session"];
				if (this.sessions.TryGetValue(session,out rpcinfo)) {

					//Console.WriteLine("remove session {0}",session);
					this.sessions.Remove(session);

				}
				SprotoHelper.Assert(rpcinfo != null,"unknow session");
				rpcinfo.type = "response";
				SprotoProtocol protocol = this.server.GetProtocol(rpcinfo.tag);
				if (protocol.response != null) {

					SprotoObject response = this.server.Decode(protocol.response,this.reader);
					rpcinfo.response = response;
				}
			}
			return rpcinfo;
		}

		private SprotoObject NewPackageHeader(SprotoMgr sprotomgr,UInt16 tag,Int64 session) {
			SprotoObject header = sprotomgr.NewSprotoObject("package");
			if (tag != 0) { // tag == 0 mean : response header
				header["type"] = tag;
			} else {
				SprotoHelper.Assert(session != 0,"response expect session");
			}
			if (session != 0)
				header["session"] = session;
			return header;
		}
	}
}
