using System;
using System.Collections.Generic;
using UnityEngine;

namespace Realm
{
    [Serializable]
    public class StateMessage
    {
        public string type;
        public GameState state;
    }

    [Serializable]
    public class ErrorMessage
    {
        public string type;
        public string message;
    }

    [Serializable]
    public class GameState
    {
        public string roomCode;
        public string phase;
        public int playerTarget;
        public int discussionSeconds;
        public int[] discussionVotes;
        public string[] selected;
        public PublicPlayer[] players;
        public bool host;
        public int round;
        public int turn;
        public int required;
        public DieRoll lastRoll;
        public ChatMessage[] chat;
        public string[] log;
        public YouState you;
        public ActionState action;
    }

    [Serializable]
    public class PublicPlayer
    {
        public int index;
        public string name;
        public int handCount;
        public int selectedCount;
        public CardData[] publicDiscard;
        public int finalDiscardCount;
        public string role;
    }

    [Serializable]
    public class YouState
    {
        public int index;
        public string name;
        public string role;
        public CardData[] hand;
        public bool canDiscard;
        public bool canAct;
    }

    [Serializable]
    public class CardData
    {
        public string id;
        public string type;
        public int round;
    }

    // The server sends the discard die as an object, not a bare number.
    // Faces are 0 · 1 · 1 · 2 · 2 · 3.
    [Serializable]
    public class DieRoll
    {
        public string id;
        public int playerIndex;
        public int value;
        public int faceIndex;
        public int round;
        public long rolledAt;
    }

    [Serializable]
    public class ActionState
    {
        public string role;
        public int playerIndex;
    }

    [Serializable]
    public class ChatMessage
    {
        public string id;
        public int playerIndex;
        public string name;
        public string text;
        public int round;
        public long sentAt;
    }
}
