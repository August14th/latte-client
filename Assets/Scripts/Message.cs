using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class Message
{
    public Message(int command, MsgType type, MapBean body)
    {
        Type = type;
        Command = command;
        Body = body;
    }

    public readonly MsgType Type;

    public readonly int Command;

    public readonly MapBean Body;

    public byte[] Encode()
    {
        byte[] body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Body));
        byte[] bytes = new byte[4 + 4 + 1 + 4 + body.Length];
        BinaryWriter writer = new BinaryWriter(new MemoryStream(bytes));
        try
        {
            // 消息序号
            writer.Write(IPAddress.HostToNetworkOrder(0));
            // 协议号
            writer.Write(IPAddress.HostToNetworkOrder(Command));
            // 消息类型
            writer.Write((byte) Type);
            // body长度
            writer.Write(IPAddress.HostToNetworkOrder(body.Length));
            // 消息体
            writer.Write(body);

            return bytes;
        }
        finally
        {
            writer.Close();
        }
    }

    public static Message Decode(byte[] bytes)
    {
        BinaryReader reader = new BinaryReader(new MemoryStream(bytes));
        try
        {
            // 消息序号
            var id = IPAddress.NetworkToHostOrder(reader.ReadInt32());
            // 协议号
            var command = IPAddress.NetworkToHostOrder(reader.ReadInt32());
            // 消息类型
            var type = (MsgType) reader.ReadByte();
            // 消息体长度p
            var length = IPAddress.NetworkToHostOrder(reader.ReadInt32());
            // 消息体
            var body = new MapBean(Encoding.UTF8.GetString(reader.ReadBytes(length)));
            switch (type)
            {
                case MsgType.Response:
                case MsgType.Exception:
                    return new Message(command, type, body);
                case MsgType.Event:
                    return new EventX(command, body);
                default:
                    throw new Exception("invalid message type:" + type);
            }
        }
        finally
        {
            reader.Close();
        }
    }
}

public class Request : Message
{
    private MapBean _response;

    private string _error;

    public void SetResponse(Message msg)
    {
        if (msg.Type == MsgType.Response)
            _response = msg.Body;
        else if (msg.Type == MsgType.Exception)
            _error = (string) msg.Body["err"];
        else
            throw new Exception("invalid response:" + msg.Type);
    }

    public bool IsDone()
    {
        return _response != null || _error != null;
    }

    public MapBean GetResponse()
    {
        if (_error != null) throw new Exception(_error);
        return _response;
    }

    public Request(int command, MapBean body) : base(command, MsgType.Request, body)
    {
    }
}

public class EventX : Message
{
    public EventX(int command, MapBean body) : base(command, MsgType.Event, body)
    {
    }
}

public enum MsgType
{
    Request = 0,
    Response = 1,
    Exception = 2,
    Event = 3
}

public class MapBean : Dictionary<string, object>
{
    public MapBean() : base(0, null)
    {
    }

    public int GetInt(string key)
    {
        return Convert.ToInt32(this[key]);
    }

    public string GetString(string key)
    {
        return (string) this[key];
    }

    public MapBean(string jsonString)
    {
        var json = JsonConvert.DeserializeObject<MapBean>(jsonString);
        foreach (var keyValue in json)
        {
            object value = keyValue.Value;
            if (value is JObject)
            {
                value = new MapBean(value.ToString());
            }
            else if (value is long)
            {
                if ((long) value < int.MaxValue)
                {
                    value = Convert.ToInt32((long) value);
                }
            }

            Add(keyValue.Key, value);
        }
    }

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }
}