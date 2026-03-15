using System;
using System.Collections.Generic;
using System.Linq;
using MiniGames.BlazorGames.BrokenForge.Models;

namespace MiniGames.BlazorGames.BrokenForge.Services
{
    public class GameService
    {
        private readonly GameState _state;
        private readonly Random _rng;

        public GameService(GameState state, int? seed = null)
        {
            _state = state;
            _rng = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public Weapon ForgeWeapon(string name, int damage, int cost)
        {
            if (_state.Player.Coins < cost)
                throw new InvalidOperationException("Not enough coins.");

            _state.Player.Coins -= cost;
            var weapon = new Weapon
            {
                Name = name,
                Damage = damage,
                Durability = 100,
                Value = cost
            };
            _state.Player.Inventory.Add(weapon);
            return weapon;
        }

        public void BuyItem(Item item, int price)
        {
            if (_state.Player.Coins < price)
                throw new InvalidOperationException("Not enough coins.");

            _state.Player.Coins -= price;
            _state.Player.Inventory.Add(item);
        }

        public void EquipWeapon(Weapon weapon)
        {
            if (!_state.Player.Inventory.Contains(weapon))
                throw new InvalidOperationException("Weapon not in inventory.");

            _state.Player.EquippedWeapon = weapon;
        }

        public void AttackEnemy(Enemy enemy)
        {
            if (!enemy.IsAlive) return;

            if (_state.Player.EquippedWeapon == null)
                throw new InvalidOperationException("No weapon equipped.");

            int damage = _state.Player.EquippedWeapon.Damage - enemy.Defense;
            if (damage < 1) damage = 1;

            enemy.Health -= damage;
            _state.Player.EquippedWeapon.Durability -= 1;

            if (_state.Player.EquippedWeapon.Durability <= 0)
            {
                _state.Player.Inventory.Remove(_state.Player.EquippedWeapon);
                _state.Player.EquippedWeapon = null;
            }

            if (!enemy.IsAlive)
            {
                _state.Player.Coins += _rng.Next(5, 15);
            }
        }

        public void EnemyTurn()
        {
            foreach (var enemy in _state.Enemies.Where(e => e.IsAlive))
            {
                if (Math.Abs(enemy.PositionX - _state.Player.PositionX) <= 1 &&
                    Math.Abs(enemy.PositionY - _state.Player.PositionY) <= 1)
                {
                    int damage = enemy.Attack;
                    _state.Player.Health -= damage;
                    if (_state.Player.Health < 0) _state.Player.Health = 0;
                }
                else
                {
                    if (enemy.PositionX < _state.Player.PositionX) enemy.PositionX++;
                    else if (enemy.PositionX > _state.Player.PositionX) enemy.PositionX--;

                    if (enemy.PositionY < _state.Player.PositionY) enemy.PositionY++;
                    else if (enemy.PositionY > _state.Player.PositionY) enemy.PositionY--;
                }
            }

            _state.Enemies.RemoveAll(e => !e.IsAlive);
        }

        public void SpawnEnemy()
        {
            int x, y;
            do
            {
                x = _rng.Next(0, _state.WorldWidth);
                y = _rng.Next(0, _state.WorldHeight);
            } while (Math.Abs(x - _state.Player.PositionX) < 5 && Math.Abs(y - _state.Player.PositionY) < 5);

            _state.Enemies.Add(new Enemy
            {
                Health = _rng.Next(20, 41),
                MaxHealth = 40,
                Attack = _rng.Next(3, 8),
                Defense = _rng.Next(1, 4),
                PositionX = x,
                PositionY = y
            });
        }
    }
}