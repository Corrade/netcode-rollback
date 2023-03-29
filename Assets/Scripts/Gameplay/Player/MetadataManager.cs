using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;

using Rollback;

namespace Rollback
{
    public class MetadataManager : MonoBehaviour
    {
        public const int MaxLives = 5;

        public int Id { get; private set; } = 0;
        public string Name { get; private set; } = "placeholder";
        public int Lives { get; private set; } = MaxLives;
        public bool IsDefeated => (Lives <= 0);

        public event Action<MetadataManager> LifeLost;
        public event Action<MetadataManager> MetadataUpdated;

        public void Initialise(int id, string name)
        {
            Assert.IsTrue(id == 0 || id == 1);

            Id = id;
            Name = name;
            ResetLives();

            MetadataUpdated?.Invoke(this);
        }

        public void ResetLives()
        {
            Lives = MaxLives;
            MetadataUpdated?.Invoke(this);
        }

        public void LoseLife()
        {
            Lives = Math.Max(Lives - 1, 0);
            LifeLost?.Invoke(this);
            MetadataUpdated?.Invoke(this);
        }
    }
}
