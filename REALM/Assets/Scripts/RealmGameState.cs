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
        public int accusedCount;
        public YouState you;
        public ActionState action;
        public ResultEntry[] results;
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
        public bool canAccuse;
        public int accuseQuota;
    }

    // One row of the final scoreboard. `total` is already the sum of base,
    // bonus and accusePoints; the parts are sent so the breakdown can be shown.
    [Serializable]
    public class ResultEntry
    {
        public int index;
        public string name;
        public string role;
        public CardData[] hand;
        public int finalDiscardCount;
        public bool success;
        // `base` is a C# keyword; `@base` declares a field whose real name is
        // "base", which is what JsonUtility matches against the server payload.
        public int @base;
        public int bonus;
        public int accusePoints;
        public int accuseCorrect;
        public int exposedBy;
        public int total;
        public string detail;
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
