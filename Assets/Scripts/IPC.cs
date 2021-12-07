using System;
using System.Text;
using System.Threading;
using System.Collections;
using UnityEngine;
using Unity.WebRTC;
using WebSocketSharp;
using ViveSR.anipal.Eye;

public class GlobalEyeData {
    public bool hit;
    public Vector3 leftOrigin;
    public Vector3 leftDirection;
    public Vector3 rightOrigin;
    public Vector3 rightDirection;
    public Vector3 focusPoint;
    public Vector3 expectedDirection;
    public float targetAngularPosition;
    public float strabismusDegree;
}
public class RoutedEyeData {
    public string leftOrigin;
    public string leftDirection;
    public string rightDirection;
    public string rightOrigin;
    public string focusPoint;
    public string expectedDirection;
    public string targetAngularPosition;
    public string strabismusDegree;
    public bool hit;
}

public class RoutedControlData {
    public int controlNo;
    public float angle;
    public float distance;
    public float speed;
    public int baseEyeIndex;
}

public class IPC : MonoBehaviour
{

    [SerializeField] private Camera cam;
    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private int portNo;
    [SerializeField] private bool active;

    public static IPC Instance;

    private WebSocket ws;
    private bool dcOpened;

    private RTCPeerConnection pc;
    private DelegateOnIceCandidate onIceCandidate;
    private DelegateOnIceConnectionChange onIceConnectionChange;
    private EventHandler<MessageEventArgs> onWebSocketMessage;

    private VideoStreamTrack videoStreamTrack;
    private RTCDataChannel dataChannel;
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

    public IPC(): base() {
        Instance = this;
    }

    void Awake()
    {
        mainThreadSyncCtx = SynchronizationContext.Current;
        if (active) {
            InitializeWebRTC();
            InitializeWebSocket();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        dcOpened = false;
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

    private void OnDCMessage(byte[] bytes) {
        string message = Encoding.UTF8.GetString(bytes);
        Debug.Log($"Data channel received from Node: {message}");
        var control = JsonUtility.FromJson<RoutedControlData>(message);

        switch(control.controlNo) {
            case 0:
                if (control.baseEyeIndex == 0) {
                    GazeRay.Instance.SetBaseEye(GazeIndex.LEFT);
                } else {
                    GazeRay.Instance.SetBaseEye(GazeIndex.RIGHT);
                }
                Sphere.Instance.StartMove(control.distance, control.angle, control.distance);
                break;
            case 1:
                Sphere.Instance.PauseMove();
                break;
            case 2:
                Sphere.Instance.StopMove();
                break;
            default:
                Debug.Log("unknown controNO: " + control.controlNo);
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

    private RTCConfiguration GetSelectedSdpSemantics()
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
        if (state == RTCIceConnectionState.Connected) {
            ws.Close();
            Debug.Log("Close WebSocket.");
        }
    }


   private void InitializeWebRTC() {
        WebRTC.Initialize(EncoderType.Software);
        var configuration = GetSelectedSdpSemantics();
        pc = new RTCPeerConnection(ref configuration);
        pc.OnIceCandidate = OnIceCandidate;
        pc.OnIceConnectionChange = OnIceConnectionChange;
        pc.OnDataChannel = OnDataChannel;

        videoStreamTrack = cam.CaptureStreamTrack(width, height, 1000000);
        pc.AddTrack(videoStreamTrack);
        dataChannel = pc.CreateDataChannel("data", default);
        dataChannel.OnMessage = OnDCMessage;

        if (!videoUpdateStarted) {
            StartCoroutine(WebRTC.Update());
            videoUpdateStarted = true;
        }
    }

    private string GetVector3Str(Vector3 vec) {
        return $"({vec.x.ToString("F4")},{vec.y.ToString("F4")},{vec.z.ToString("F4")})" ;
    }
    public void SendEyeData(GlobalEyeData eyeData) {

        var data = new RoutedEyeData();

        data.leftDirection = GetVector3Str(eyeData.leftDirection);
        data.rightDirection = GetVector3Str(eyeData.rightDirection);
        data.leftOrigin = GetVector3Str(eyeData.leftOrigin);
        data.rightOrigin = GetVector3Str(eyeData.rightOrigin);
        data.targetAngularPosition = eyeData.targetAngularPosition.ToString("F1");
        data.hit = eyeData.hit;
        if (eyeData.hit) {
            data.focusPoint = GetVector3Str(eyeData.focusPoint);
            data.expectedDirection = GetVector3Str(eyeData.expectedDirection);
            data.strabismusDegree = eyeData.strabismusDegree.ToString("F1");
        }
        if (dcOpened) {
            dataChannel.Send(JsonUtility.ToJson(data));
        }
    }
    private void OnDataChannel(RTCDataChannel channel) {
        Debug.Log("OnDataChannel");
        dataChannel = channel;
        dataChannel.OnMessage = OnDCMessage;
        dcOpened = true;
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
