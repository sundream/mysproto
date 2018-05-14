using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace Sproto {
	public class Token {
		public string type;
		public string val;
		public int line;
		public int column;

		public Token (string type,string val,int line,int column) {
			this.type = type;
			this.val = val;
			this.line = line;
			this.column = column;
		}
	}

	public class Lexer {
		private static string range(char a,char b) {
			int len = b - a + 1;
			char[] chars = new char[len];
			for (int i=0; i < len; i++) {
				chars[i] = (char)(a + i);
			}
			return new string(chars);
		}

		private string words = range('a','z') + range('A','Z') + "_";
		private string numbers = range('0','9');
		private string spaces = " \t\r\n";

		private int line;
		private int pos;
		private int line_pos;
		private char lastchar;
		private string filename;
		private string text;
		private int length;
		public List<Token> tokens;


		public Lexer (string text,string filename="=text") {
			this.text = text;
			this.filename = filename;
			this.line = 1;
			this.pos = 0;
			this.line_pos = 0;
			this.lastchar = '\0';
			this.length = this.text.Length;
			this.init();
		}
		
		public string error_pos (int line,int column,SprotoType type=null) {
			string str = String.Format("{0}({1}:{2}): ",this.filename,line,column);
			if (type != null) {
				str = str + String.Format("type={0}: ", type.name);
			}
			return str;
		}
		public delegate bool FuncCharIn (char c);
		public bool isalpha (char c) {
			return this.words.IndexOf(c) != -1;
		}
		public bool isdigit (char c) {
			return this.numbers.IndexOf(c) != -1;
		}
		public bool isspace (char c) {
			return this.spaces.IndexOf(c) != -1;
		}
		public char readchar () {
			char c = this.text[this.pos];
			if ('\n' == c) {
				this.line = this.line + 1;
				this.line_pos = this.pos;
			} else if ('\r' == this.lastchar) {
				this.line = this.line + 1;
				this.line_pos = this.pos;
			}
			this.pos++;
			this.lastchar = c;
			return c;
		}
		public char peek (int pos) {
			return this.text[pos];
		}
		public string peekutil (FuncCharIn stop) {
			List<char> chars = new List<char>();
			int pos = this.pos;
			while (pos < this.length) {
				char c = this.peek(pos);
				if (stop(c))
					break;
				pos++;
				chars.Add(c);
			}
			return new string (chars.ToArray ());
		}

		public string readutil (FuncCharIn stop) {
			List<char> chars = new List<char>();
			while (this.pos < this.length) {
				char c = this.peek(this.pos);
				if (stop(c))
					break;
				this.readchar();
				chars.Add(c);
			}
			return new string (chars.ToArray ());
		}
		/*
		public void seek (int pos) {
			if (pos < 0 || pos >= this.length)
				SprotoHelper.Error("{0}: seek invalid pos: {1}",this.filename,pos);
			this.pos = pos;
		}
		*/

		public void check_word_size (string word,int max_size,int line,int column) {
			if (word.Length >= max_size)
				SprotoHelper.Error(this.error_pos(line,column) + "word '{0}' size > {0}",word,max_size);
		}

		public IEnumerable<Token> gen_next_token () {
			while (this.pos < this.length) {
				int line = this.line;
				int column = this.pos - this.line_pos;
				char ch = this.peek(this.pos);
				if (this.isalpha(ch)) {
					string word = this.readutil(c => {
								if (!(this.isalpha(c) ||
									  this.isdigit(c)))
									return true;
								return false;
							});
					this.check_word_size(word,256,line,column);
					yield return new Token("word",word,line,column);
				} else if (this.isdigit(ch)) {
					string number = this.readutil(c => {
								if (!this.isdigit(c))
									return true;
								return false;
							});
					yield return new Token("tag",number,line,column);
				} else if (this.isspace(ch)) {
					string space = this.readutil(c => {
								if (!this.isspace(c))
									return true;
								return false;
							});
					yield return new Token("space",space,line,column);
				} else if ('.' == ch ) {
					this.readchar();
					yield return new Token("point",".",line,column);
				} else if (':' == ch ) {
					this.readchar();
					yield return new Token("colon",":",line,column);
				} else if ('{' == ch ) {
					this.readchar();
					yield return new Token("block_start","{",line,column);
				} else if ('}' == ch ) {
					this.readchar();
					yield return new Token("block_end","}",line,column);
				} else if ('(' == ch ) {
					this.readchar();
					yield return new Token("key_start","(",line,column);
				} else if (')' == ch ) {
					this.readchar();
					yield return new Token("key_end",")",line,column);
				} else if ('*' == ch ) {
					this.readchar();
					yield return new Token("star","*",line,column);
				} else if ('#' == ch ) {
					string comment = this.readutil(c => {
								if ('\n' == c)
									return true;
								return false;
							});
					yield return new Token("comment",comment,line,column);
				} else {
					SprotoHelper.Error(this.error_pos(line,column) + "unknow token:'{0}'",ch);
				}
			}
		}

		private void init () {
			this.tokens = new List<Token>();
			foreach (Token token in this.gen_next_token()) {
				if (!(token.type == "comment")) {
					this.tokens.Add(token);
				}
			}
			this.tokens.Add(new Token("eof","",-1,-1));
		}

		public Token pop_token () {
			Token token = this.tokens[0];
			this.tokens.RemoveAt(0);
			return token;
		}
	}

	public static class SprotoParser {
		// cloudwu's sproto tag is limit sizeof(UInt16)/2-1
		private const UInt16 MAX_FIELD_TAG = 0x7fff;
		private const UInt16 MAX_DECIMAL = 9;
		private static void _Parse (SprotoMgr sprotomgr,string proto,string filename) {
			Lexer lexer = new Lexer(proto,filename);
			while (lexer.tokens.Count > 0) {
				Token token = lexer.tokens[0];
				if ("eof" == token.type)
					break;
				switch (token.type) {
					case "word":
						SprotoProtocol protocol = parse_protocol(lexer,sprotomgr);
						sprotomgr.AddProtocol(protocol);
						break;
					case "point":
						lexer.pop_token();
						SprotoType type = parse_type(lexer);
						sprotomgr.AddType(type);
						break;
					case "space":
						ignore(lexer,"space");
						break;
					default:
						SprotoHelper.Error(lexer.error_pos(token.line,token.column) + "invalid token:<{0},{1}>",token.type,token.val);
						break;
				}
			}
		}

		public static SprotoMgr Parse (string proto,string filename="=text") {
			SprotoMgr sprotomgr = new SprotoMgr();
			SprotoParser._Parse(sprotomgr,proto,filename);
			sprotomgr.Check();
			return sprotomgr;
		}

		private static void _ParseFile(SprotoMgr sprotomgr,string filename) {
			FileStream stream = new FileStream(filename,FileMode.Open);
			StringBuilder sb = new StringBuilder();
			byte[] buf = new byte[1024];
			int len = stream.Read(buf,0,buf.Length);
			while (len > 0) {
				sb.Append(Encoding.UTF8.GetString(buf,0,len));
				len = stream.Read(buf,0,buf.Length);
			}
			stream.Close();
			string proto = sb.ToString();
			SprotoParser._Parse(sprotomgr,proto,filename);
		}

		public static SprotoMgr ParseFile(string filename) {
			return SprotoParser.ParseFile(new List<string>{filename});
		}

		public static SprotoMgr ParseFile(List<string> filenames) {
			SprotoMgr sprotomgr = new SprotoMgr();
			foreach (string filename in filenames) {
				SprotoParser._ParseFile(sprotomgr,filename);
			}
			sprotomgr.Check();
			return sprotomgr;
		}

		private static SprotoProtocol parse_protocol (Lexer lexer,SprotoMgr sprotomgr) {
			SprotoProtocol protocol = new SprotoProtocol();
			Token token = expect(lexer,"word","space");
			protocol.name = token.val;
			// keep same behavior with cloudwu's sproto
			// allow { follow by protocol's tag, eg: protocol tag{
			token = expect(lexer,"tag");
			ignore(lexer,"space");
			protocol.tag = Convert.ToUInt16(token.val);
			if (protocol.tag >= SprotoParser.MAX_FIELD_TAG)
				SprotoHelper.Error(lexer.error_pos(token.line,token.column) + "{0} protocol's tag {1} >= {2}",protocol.name,protocol.tag,SprotoParser.MAX_FIELD_TAG);
			
			expect(lexer,"block_start","space");
			while (true) {
				token = lexer.tokens[0];
				if ("eof" == token.type || "block_end" == token.type)
					break;
				token = expect(lexer,"word","space");
				if (token.val == "request" || token.val == "response") {
					string subprotocol_type = null;
					Token token2 = expect(lexer,"word|block_start","space");
					if (token2.type == "word") {
						if (token2.val != "nil") {
							subprotocol_type = token2.val;
						}
					} else {
						SprotoType typedef = new SprotoType();
						typedef.name = String.Format("{0}.{1}",protocol.name,token.val);
						_parse_type(lexer,typedef);
						sprotomgr.AddType(typedef);
						subprotocol_type = typedef.name;
					}
					if (subprotocol_type != null) {
						if (token.val == "request") {
							protocol.request = subprotocol_type;
						} else {
							protocol.response = subprotocol_type;
						}
					}
				} else {
					SprotoHelper.Error(lexer.error_pos(token.line,token.column) + "{0}: invalid subprotocol:{1}",protocol.name,token.val);
				}
			}
			expect(lexer,"block_end","space|eof");
			return protocol;
		}

		private static SprotoType parse_type (Lexer lexer) {
			SprotoType type = new SprotoType();
			// keep same behavior with cloudwu's sproto
			// allow { follow by type, eg: .typedef{
			Token token = expect(lexer,"word");
			ignore(lexer,"space");
			type.name = token.val;
			expect(lexer,"block_start","space");
			_parse_type(lexer,type);
			return type;
		}

		private static SprotoType _parse_type (Lexer lexer,SprotoType type) {
			while (true) {
				Token token = lexer.tokens[0];
				if ("eof" == token.type || "block_end" == token.type)
					break;
				if (token.type == "point") {
					lexer.pop_token();
					SprotoType nest_type = parse_type(lexer);
					type.AddType(nest_type);
				} else {
					type.AddField(parse_field(lexer,type));
				}
			}
			expect(lexer,"block_end","space|eof");
			return type;
		}

		private static SprotoField parse_field (Lexer lexer,SprotoType type) {
			SprotoField field = new SprotoField();
			Token token = expect(lexer,"word","space");
			field.name = token.val;
			// allow colon follow by tag,eg: field tag: type
			token = expect(lexer,"tag");
			ignore(lexer,"space");
			field.tag = Convert.ToUInt16(token.val);
			if (field.tag >= SprotoParser.MAX_FIELD_TAG)
				SprotoHelper.Error(lexer.error_pos(token.line,token.column,type) + "{0} field's tag {1} >= {2}",field.name,field.tag,SprotoParser.MAX_FIELD_TAG);
			expect(lexer,"colon","space");
			token = optional(lexer,"star");
			if (token != null) {
				field.is_array = true;
			}
			token = expect(lexer,"word");
			field.type = token.val;
			string fieldtype = field.type;
			token = optional(lexer,"key_start");
			if (token != null) {
				// allow field tag : type(space+key+space)
				ignore(lexer,"space");
				token = expect(lexer,"word|tag");
				if ("tag" == token.type) {
					if (fieldtype != "integer")
						SprotoHelper.Error(lexer.error_pos(token.line,token.column,type) + "decimal index expect 'integer' got '{0}'",fieldtype);
					field.digit = Convert.ToUInt16(token.val);
					if (field.digit > SprotoParser.MAX_DECIMAL)
						SprotoHelper.Error(lexer.error_pos(token.line,token.column,type) + "decimal index {0} > {1}",field.digit,SprotoParser.MAX_DECIMAL);
				} else {
					if (SprotoHelper.IsBuildInType (fieldtype)) {
						SprotoHelper.Error(lexer.error_pos(token.line,token.column,type) + "map index expect 'usertype' got '{0}'",fieldtype);
					}
					field.key = token.val;
				}
				ignore(lexer,"space");
				expect(lexer,"key_end","space");
			} else {
				expect(lexer,"space");
				ignore(lexer,"space");
			}
			return field;
		}

		private static Token expect (Lexer lexer,string need,string need_and_ignore=null) {
			string[] types = need.Split('|');
			Token token = lexer.pop_token();
			if (-1 == Array.IndexOf(types,token.type))
				SprotoHelper.Error(lexer.error_pos(token.line,token.column) + "token expect type '{0}',got '{1}' {2}",need,token.type,token.val);
			if (need_and_ignore != null) {
				// expect at least one
				expect(lexer,need_and_ignore);
				ignore(lexer,need_and_ignore);
			}
			return token;
		}

		private static Token optional (Lexer lexer,string need) {
			string[] types = need.Split('|');
			Token token = lexer.tokens[0];
			if (-1 == Array.IndexOf(types,token.type))
				return null;
			lexer.pop_token();
			return token;
		}

		private static void ignore (Lexer lexer,string _ignore) {
			string[] types = _ignore.Split('|');
			while (true) {
				Token token = lexer.tokens[0];
				if (-1 == Array.IndexOf(types,token.type)) {
					break;
				}
				if (token.type != "eof") {
					lexer.pop_token();
				} else {
					break;
				}
			}
		}
	}
}
