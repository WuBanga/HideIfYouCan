﻿using Scripts.Components;
using Scripts.Exceptions;
using Scripts.Management.Network;

using UnityEngine;

using Random = UnityEngine.Random;

namespace Scripts.Management.Game
{
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private GameSettings gameSettings = new GameSettings();

        private GameState gameState;

        private int hidersCount;
        private int seekersCount;

        private float time;

        private void Start()
        {
            gameState = GameState.Starting;
        }

        private void FixedUpdate()
        {
            if (gameState == GameState.Finished || gameState == GameState.Starting)
                return;

            time += Time.fixedDeltaTime;

            switch (gameState)
            {
                case GameState.FreezeTime:
                    if (time >= gameSettings.freezeTime)
                    {
                        foreach (var player in ServerManager.GetAllPlayers())
                            player.RpcStartGame();

                        gameState = GameState.HideTime;
                        time      = 0f;

                        Debug.Log("Freeze time expired");
                    }

                    break;
                case GameState.HideTime:
                    if (time >= gameSettings.hideTime)
                    {
                        // TODO: Enable seekers

                        gameState = GameState.SeekTime;
                        time      = 0f;

                        Debug.Log("Time to hide has ended");
                    }

                    break;
                case GameState.SeekTime:
                    if (time >= gameSettings.seekTime)
                    {
                        // TODO: E.g. show game summary

                        gameState = GameState.Ending;
                        time      = 0f;

                        Debug.Log("Round ended");
                    }

                    break;
                case GameState.Ending:
                    if (time >= gameSettings.endingTime)
                    {
                        // TODO: E.g switch map or close server

                        gameState = GameState.Finished;
                    }

                    break;
            }
        }

        public Role AssignRole()
        {
            if ((float) seekersCount / (hidersCount + 1) < gameSettings.seekersToHidersRelation)
            {
                if (Random.value >= .5f)
                {
                    hidersCount++;

                    return Role.Hider;
                }

                seekersCount++;

                return Role.Seeker;
            }

            hidersCount++;

            return Role.Hider;
        }

        public void UnassignRole(Role role)
        {
            switch (role)
            {
                case Role.Hider:
                    hidersCount--;

                    break;
                case Role.Seeker:
                    seekersCount--;

                    break;
                default: throw new UnhandledRoleException(role);
            }
        }

        public void StartGame()
        {
            gameState = GameState.FreezeTime;

            Debug.Log("Game started");
        }
    }
}