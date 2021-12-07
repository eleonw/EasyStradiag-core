using System;
using System.Threading;
using System.Collections;
using UnityEngine;
using Unity.WebRTC;
using WebSocketSharp;

public class VideoStreaming : MonoBehaviour
{

    [SerializeField] private Camera cam;
    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private int portNo;

    private WebSocket ws;

    private RTCPeerConnection pc;
    private DelegateOnIceCandidate onIceCandidate;
    private DelegateOnIceConnectionChange onIceConnectionChange;
    private EventHandler<MessageEventArgs> onWebSocketMessage;

    private VideoStreamTrack videoStreamTrack;
    private bool videoUpdateStarted;
    private RTCOfferOptions _offerOptions = new RTCOfferOptions
    {
        iceRestart = false,
        offerToReceiveAudio = false,
        offerToReceiveVideo = true
    };
    private RTCAnswerOptions _answerOptions = new RTCAnswerOptions { iceRestart = false, };

    private SynchronizationContext mainThreadSyncCtx;
    private SendOrPostCallback onOfferCallBack;

    void Awake()
    {
        mainThreadSyncCtx = SynchronizationContext.Current;
        InitializeWebRTC();
        InitializeWebSocket();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnDestroy()
    {
        WebRTC.Dispose();
    }

    private void InitializeWebSocket()
    {
        Debug.Log("Initialing WebSocket...");
        ws = new WebSocket($"ws://localhost:{portNo}");
        ws.OnMessage += OnWSMessage;
        ws.OnClose += (s, msg) => { Debug.Log("WebSocket close"); };
        ws.Connect();

        GreetNode();
    }

    private void OnWSMessage(object sender, MessageEventArgs args) {
        var message = JsonUtility.FromJson<SignalingMessage>(args.Data);
        var data = message.data;
        switch (message.type) {
            case "message":
                Debug.Log($"Receive a message from Node: {data.message}");
                break;
            case "offer":
                Debug.Log("Receive an offer from Node");
                var offerDesc = new RTCSessionDescription {
                    type = RTCSdpType.Offer,
                    sdp = data.sdp
                };
                onOfferCallBack = state => { StartCoroutine(OnOffer(offerDesc));};
                mainThreadSyncCtx.Post(onOfferCallBack, null);
                break;
            case "candidate":
                var candidateInfo = new RTCIceCandidateInit {
                    candidate = data.candidate,
                    sdpMid = data.sdpMid,
                    sdpMLineIndex = data.sdpMLineIndex
                };
                pc.AddIceCandidate(new RTCIceCandidate(candidateInfo));
                break;
            default:
                Debug.Log($"Receive unknown message type: {message.type}");
                break;
        }
    }

    private void GreetNode() {
        var message = new SignalingMessage {
            type = "message",
            data = new SignalingMessageData {
                message = "Hello Node!"
            }
        };
        SendMessage(message);

        var message1 = new SignalingMessage {
            type = "start"
        };
        SendMessage(message1);
    }

    private static RTCConfiguration GetSelectedSdpSemantics()
    {
        RTCConfiguration config = default;
        config.iceServers = new[] { new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } } };

        return config;
    }

    private void SendMessage(SignalingMessage msg)
    {
        Debug.Log($"Send a(an) \"{msg.type}\" to Node:\n {JsonUtility.ToJson(msg.data, true)}");
        ws.Send(JsonUtility.ToJson(msg));
    }

    private void OnIceCandidate(RTCIceCandidate candidate)
    {
        var message = new SignalingMessage();
        var data = new SignalingMessageData();
        data.candidate = candidate.Candidate;
        data.sdpMid = candidate.SdpMid;
        data.sdpMLineIndex = candidate.SdpMLineIndex;
        message.type = "candidate";
        message.data = data;
        SendMessage(message);
    }

    private void OnIceConnectionChange(RTCIceConnectionState state)
    {
        Debug.Log($"IceConnection state changed to {state}");
    }


    private void InitializeWebRTC() {
        WebRTC.Initialize(EncoderType.Software);
        var configuration = GetSelectedSdpSemantics();
        pc = new RTCPeerConnection(ref configuration);
        pc.OnIceCandidate = OnIceCandidate;
        pc.OnIceConnectionChange = OnIceConnectionChange;

        videoStreamTrack = cam.CaptureStreamTrack(width, height, 1000000);
        pc.AddTrack(videoStreamTrack);
        
        if (!videoUpdateStarted) {
            StartCoroutine(WebRTC.Update());
            videoUpdateStarted = true;
        }
    }


    private IEnumerator OnOffer(RTCSessionDescription desc) {
        Debug.Log("OnOffer");
        Debug.Log("SetRemoteDescription start");
        var op = pc.SetRemoteDescription(ref desc);
        yield return op;
        if (!op.IsError) {
            Debug.Log("SetRemoteDescription succeed");
        }
        else {
            Debug.Log("SetLocalDescription failed");
        }

        var op1 = pc.CreateAnswer(ref _answerOptions);
        yield return op1;
        if (!op1.IsError) {
            Debug.Log("CreateAnswer succeed");
            yield return OnCreateAnswerSuccess(op1.Desc);
        } else {
            Debug.Log("CreateAnswer failed");
        }
    }

    private IEnumerator OnCreateAnswerSuccess(RTCSessionDescription desc) {
        Debug.Log("SetLocalDescription start");
        var op = pc.SetLocalDescription(ref desc);
        yield return op;
        if (!op.IsError) {
            Debug.Log("SetLocalDescription succeed");
        }
        else {
            Debug.Log("SetLocalDescription failed");
        }
        var message = new SignalingMessage {
            type = "answer",
            data = new SignalingMessageData {
                type = "answer",
                sdp = desc.sdp
            }
        };
        SendMessage(message);
    }

    


}
