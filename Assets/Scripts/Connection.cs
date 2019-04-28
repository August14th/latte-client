using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Resources;
using UnityEngine;

public class Connection : MonoBehaviour
{
    private const int HeadSize = 13; // 消息序号4+协议号4+消息类型1+消息体长度4

    private const int BodySizeOffset = 4 + 4 + 1; // 消息体长度字段在头部的偏移

    private TcpClient _client;

    private readonly List<Message> _toSend = new List<Message>(); // 同时只能有一个请求

    public string Host = "localhost";

    public int Port = 2019;

    private void Start()
    {
        _client = new TcpClient(Host, Port);
        // 启动读循环
        StartCoroutine(Loop());
        Debug.Log("Connected to Server at:" + Host + ":" + Port);
    }

    public Coroutine Ask(Request request)
    {
        return StartCoroutine(Ask0(request));
    }

    private IEnumerator Ask0(Request request)
    {
        _toSend.Add(request);
        while (!request.IsDone())
        {
            yield return null;
        }
    }

    public void AskAsync(Request request, Action<MapBean> onSuccess, Action<Exception> onFailure)
    {
        StartCoroutine(AskAsync0(request, onSuccess, onFailure));
    }

    private IEnumerator AskAsync0(Request request, Action<MapBean> onSuccess, Action<Exception> onFailure)
    {
        yield return Ask(request);
        try
        {
            onSuccess(request.GetResponse());
        }
        catch (Exception e)
        {
            onFailure(e);
        }
    }

    public void Notify(int command, MapBean body)
    {
        _toSend.Add(new EventX(command, body));
    }

    private void Send(Message message)
    {
        BinaryWriter writer = new BinaryWriter(_client.GetStream());
        writer.Write(message.Encode());
        Debug.Log(DateTime.Now.ToString("HH:mm:ss:fff") + "C -> 0x" + string.Format("{0:x}", message.Command) +
              ":" + message.Body);
    }

    private Request Write0()
    {
        if (_toSend.Count != 0)
        {
            var message = _toSend[0];
            Send(message);
            _toSend.RemoveAt(0);

            if (message is Request)
            {
                return (Request) message;
            }
        }

        return null;
    }

    private IEnumerator Loop()
    {
        BinaryReader reader = new BinaryReader(_client.GetStream());

        while (true)
        {
            var request = Write0();
            
            if (request != null || _client.Available > 0)
            {
                while (_client.Available < HeadSize)
                    yield return null;
                // 头部
                byte[] headBuffer = reader.ReadBytes(HeadSize);
                // 消息体长度
                var bodySize = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(headBuffer, BodySizeOffset));

                while (_client.Available < bodySize)
                    yield return null;
                // 消息体
                byte[] bytes = new byte[HeadSize + bodySize];
                headBuffer.CopyTo(bytes, 0);
                reader.ReadBytes(bodySize).CopyTo(bytes, HeadSize);
                // 解码
                var message = Message.Decode(bytes);
                Debug.Log(DateTime.Now.ToString("HH:mm:ss:fff") + "S <- 0x" + string.Format("{0:x}", message.Command) +
                      ":" + message.Body);

                switch (message.Type)
                {
                    case MsgType.Response:
                    case MsgType.Exception:
                        HandleResponse(request, message);
                        break;
                    case MsgType.Event:
                        DispatchEvent((EventX) message);
                        break;
                    default:
                        throw new Exception("invalid message type:" + message.Type);
                }
            }
            else
            {
                yield return null;
            }
        }
    }

    private void HandleResponse(Request request, Message message)
    {
        if (request.Command != message.Command)
            throw new Exception("Commands not match, expected is " + request.Command + ", but is " + message.Command);
        request.SetResponse(message);
    }

    /**
     * 事件监听
     **/
    private readonly Dictionary<int, List<Action<MapBean>>> _evtHandlers = new Dictionary<int, List<Action<MapBean>>>();

    public void Register(int cmd, Action<MapBean> handler)
    {
        List<Action<MapBean>> list;
        if (_evtHandlers.TryGetValue(cmd, out list))
        {
            if (!list.Contains(handler)) list.Add(handler);
        }
        else
        {
            _evtHandlers.Add(cmd, new List<Action<MapBean>> {handler});
        }
    }

    public void DeRegister(int cmd, Action<MapBean> handler = null)
    {
        if (handler != null)
        {
            List<Action<MapBean>> list;
            if (_evtHandlers.TryGetValue(cmd, out list))
                list.Remove(handler);
        }
        else
            _evtHandlers.Remove(cmd);
    }

    private void DispatchEvent(EventX evt)
    {
        List<Action<MapBean>> list;
        if (_evtHandlers.TryGetValue(evt.Command, out list))
        {
            list.ForEach(handler => handler(evt.Body));
        }
    }
}