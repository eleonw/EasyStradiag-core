using System;



    [Serializable]
    public class SignalingMessageData
    {
        public string status;
        public string message;
        public string sessionId;
        public string connectionId;
        public string peerId;
        public string sdp;
        public string type;
        public string candidate;
        public string sdpMid;
        public int? sdpMLineIndex;
    }

    [Serializable]
    public class SignalingMessage
    {
        public string type;
        public SignalingMessageData data;
    }


    [Serializable]
    public class RoutedMessage<T>
    {
        public string from;
        public string to;
        public string type;
        public T data;
    }

    [Serializable]
    public class DescData
    {
        public string connectionId;
        public string sdp;
        public string type;
    }

    [Serializable]
    public class CandidateData
    {
        public string connectionId;
        public int sdpMLineIndex;
        public string sdpMid;
        public string candidate;
    }

    

