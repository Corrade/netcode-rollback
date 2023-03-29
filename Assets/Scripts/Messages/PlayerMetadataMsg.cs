using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DarkRift;

namespace Rollback
{
    public class PlayerMetadataMsg : IDarkRiftSerializable
    {
        public string Name { get; private set; }
        
        public PlayerMetadataMsg() {}

        public PlayerMetadataMsg(string name)
        {
            Name = name;
        }

        public static Message CreateMessage(string name)
        {
            return Message.Create(
                Tags.PlayerMetadata,
                new PlayerMetadataMsg(name)
            );
        }

        public void Deserialize(DeserializeEvent e)
        {
            Name = e.Reader.ReadString();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Name);
        }
    }
}
