using System;
using Common;
using UnityEngine;

namespace Deckbuilding.QuestRules
{
    public class KillMonster : IQuestRule
    {
        public IQuestLogic Create()
        {
            return new Logic();
        }
        
        public class Logic : IQuestLogic
        {
            private  Enemy _randomEnemy;

            public Logic()
            {
                _randomEnemy = Enemy.List[UnityEngine.Random.Range(0, Enemy.List.Count)];
                _randomEnemy.SetHighlight(true);
            }
            
            public GameObject GetTrackingObject()
            {
                if (_randomEnemy == null)
                    return null;
                
                if (_randomEnemy.IsDead())
                    return null;
                
                return _randomEnemy.gameObject;
            }

            public bool IsCompleted()
            {
                if (_randomEnemy == null)
                    throw new Exception("enemy is null");
                
                if (_randomEnemy.IsDead())
                    return true;
                
                return false;
            }

            public void Clear()
            {
                _randomEnemy.SetHighlight(false);
                _randomEnemy = null;
            }
        }
    }
}