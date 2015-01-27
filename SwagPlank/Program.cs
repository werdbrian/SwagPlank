using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace SwagPlank
{

    class Program
    {
        static Menu config;
        static List<BuffType> buffs = new List<BuffType>();
        static Spell Q = new Spell(SpellSlot.Q, 625);
        static Spell W = new Spell(SpellSlot.W);
        static Spell E = new Spell(SpellSlot.E);
        static Spell R = new Spell(SpellSlot.R);
        static Orbwalking.Orbwalker orbwalker;
        static Obj_AI_Hero player = ObjectManager.Player;
        static SpellSlot SummonerDot = player.GetSpellSlot("SummonerDot");

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += OnGameLoad;
        }

        private static void OnGameLoad(EventArgs args)
        {
            #region Menu
            config = new Menu("SwagPlank", "swagPirate", true);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            config.AddSubMenu(targetSelectorMenu);

            config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            orbwalker = new Orbwalking.Orbwalker(config.SubMenu("Orbwalking"));

            config.AddSubMenu(new Menu("Combo", "Combo"));
            config.SubMenu("Combo").AddItem(new MenuItem("useQCombo", "Use Q").SetValue(true));
            config.SubMenu("Combo").AddItem(new MenuItem("useWCombo", "Use W").SetValue(true));
            config.SubMenu("Combo").AddItem(new MenuItem("useECombo", "Use E").SetValue(true));
            config.SubMenu("Combo").AddItem(new MenuItem("useRCombo", "Use R").SetValue(true));
            config.SubMenu("Combo").AddItem(new MenuItem("useIgnite", "Use Ignite if enemy can die from it").SetValue(true));
            config.SubMenu("Combo")
                .AddItem(new MenuItem("comboActive", "Combo").SetValue(new KeyBind(32, KeyBindType.Press)));

            config.AddSubMenu(new Menu("Harass", "Harass"));
            config.SubMenu("Harass").AddItem(new MenuItem("useQHarass", "Use Q").SetValue(true));
            config.SubMenu("Harass")
                .AddItem(new MenuItem("harassActive", "Harass").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));

            config.AddSubMenu(new Menu("Farm", "Farm"));
            config.SubMenu("Farm").AddItem(new MenuItem("useQFarm", "Use Q")).SetValue(true);
            config.SubMenu("Farm")
                .AddItem(new MenuItem("farmActive", "Farm").SetValue(new KeyBind("A".ToCharArray()[0], KeyBindType.Press)));

            config.AddSubMenu(new Menu("Misc", "Misc"));
            config.SubMenu("Misc").AddItem(new MenuItem("useQFarmOrHarass", "Prioritize Q farm over harass")).SetValue(true);
            config.SubMenu("Misc").AddItem(new MenuItem("useRGlobal", "Use auto ultimate if someone could die from it")).SetValue(true);
            config.SubMenu("Misc").AddItem(new MenuItem("useWToHeal", "Don't use heal at X% health or higher")).SetValue(new Slider(90, 100, 0));
            config.SubMenu("Misc").AddItem(new MenuItem("useWToCleanseOnly", "Only use W to cleanse CC")).SetValue(false);

            config.SubMenu("Misc").AddSubMenu(new Menu("Cleanse following CCs:", "ccsMenu"));

            config.SubMenu("Misc").SubMenu("ccsMenu").AddItem(new MenuItem("bharm", "Charm")).SetValue(true);
            config.SubMenu("Misc").SubMenu("ccsMenu").AddItem(new MenuItem("fear", "Fear")).SetValue(true);
            config.SubMenu("Misc").SubMenu("ccsMenu").AddItem(new MenuItem("slow", "Slow")).SetValue(false);
            config.SubMenu("Misc").SubMenu("ccsMenu").AddItem(new MenuItem("stun", "Stun")).SetValue(true);
            config.SubMenu("Misc").SubMenu("ccsMenu").AddItem(new MenuItem("polymorph", "Polymorph")).SetValue(true);
            config.SubMenu("Misc").SubMenu("ccsMenu").AddItem(new MenuItem("taunt", "Taunt")).SetValue(true);
            config.SubMenu("Misc").SubMenu("ccsMenu").AddItem(new MenuItem("blind", "Blind")).SetValue(true);
            config.SubMenu("Misc").SubMenu("ccsMenu").AddItem(new MenuItem("blindDescibtion",
                "Only cleanse blinds if in AA-Range"));

            config.AddSubMenu(new Menu("Drawing", "Drawing"));
            config.SubMenu("Drawing").AddItem(new MenuItem("drawQRange", "Draw Q range")).SetValue(true);

            config.AddToMainMenu();
            #endregion

            Game.PrintChat("<font color = \"#26e070\">SwagPlank loaded</font>");


            buffs.Add(BuffType.Slow);
            buffs.Add(BuffType.Taunt);
            buffs.Add(BuffType.Stun);
            buffs.Add(BuffType.Polymorph);
            buffs.Add(BuffType.Fear);
            buffs.Add(BuffType.Charm);

            Drawing.OnDraw += OnDraw;
            Game.OnGameUpdate += OnGameUpdate;
        }

        private static void OnDraw(EventArgs args)
        {
            if (config.SubMenu("Drawing").Item("drawQRange").GetValue<bool>())
                Render.Circle.DrawCircle(ObjectManager.Player.Position, 625, System.Drawing.Color.White);
        }

        private static void OnGameUpdate(EventArgs args)
        {
            bool comboActive = config.SubMenu("Combo").Item("comboActive").GetValue<KeyBind>().Active;
            bool harassActive = config.SubMenu("Harass").Item("harassActive").GetValue<KeyBind>().Active;

            var target = (TargetSelector.GetTarget(1000, TargetSelector.DamageType.Physical) ??
               TargetSelector.GetTarget(1000, TargetSelector.DamageType.Magical)) ?? TargetSelector.GetTarget(1000, TargetSelector.DamageType.True);


            if (comboActive)
            {
                if (config.SubMenu("Combo").Item("useQCombo").GetValue<bool>())
                {
                    if (target != null)
                        Q.CastOnUnit(target, true);
                }
                if (config.SubMenu("Combo").Item("useRCombo").GetValue<bool>())
                {
                    if (target != null && R.CanCast(target))
                        R.Cast(new Vector2(target.Position.X, target.Position.Y), true);
                }
                if (target != null)
                {
                    if (SummonerDot != SpellSlot.Unknown &&
                        player.Distance(target.Position) <= 600 &&
                        config.SubMenu("Combo").Item("useIgnite").GetValue<bool>())
                    {
                        if (player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) > target.Health)
                        {
                            player.Spellbook.CastSpell(SummonerDot, target);
                        }
                    }
                }
                if (config.SubMenu("Combo").Item("useECombo").GetValue<bool>())
                {
                    E.Cast(true);
                }
                if (config.SubMenu("Combo").Item("useWCombo").GetValue<bool>())
                {
                    if (player.Health < (player.MaxHealth / 100) * config.SubMenu("Misc").Item("useWToHeal").GetValue<Slider>().Value)
                    {
                        if (!config.SubMenu("Misc").Item("useWToCleanseOnly").GetValue<bool>())
                            W.Cast(true);
                    }
                }
            }
            else if (harassActive)
            {
                if (config.SubMenu("Misc").Item("useQFarmOrHarass").GetValue<bool>())
                {
                    //farm
                    if (Q.Level > 0)
                    {
                        int[] QmanaCost = new int[5] { 50, 55, 60, 65, 70 };

                        if (config.SubMenu("Farm").Item("farmActive").GetValue<KeyBind>().Active &&
                            player.Mana >= QmanaCost[Q.Level - 1] * 2)
                        {
                            foreach (var minion in MinionManager.GetMinions(625f, MinionTypes.All, MinionTeam.Enemy).
                                Where(x => Q.GetDamage(x) >= x.Health))
                            {
                                if (Q.CanCast(minion))
                                    Q.Cast(minion, true);
                            }
                        }
                    }
                }
                else
                {
                    //harass
                    if (target != null)
                        Q.CastOnUnit(target, true);
                }

            }
            else if (config.SubMenu("Farm").Item("farmActive").GetValue<KeyBind>().Active)
            {
                //farm
                if (Q.Level > 0)
                {
                    int[] QmanaCost = new int[5] { 50, 55, 60, 65, 70 };

                    if (player.Mana >= QmanaCost[Q.Level - 1] * 2)
                    {
                        foreach (var minion in MinionManager.GetMinions(625f, MinionTypes.All, MinionTeam.Enemy).
                            Where(x => Q.GetDamage(x) >= x.Health))
                        {
                            if (Q.CanCast(minion))
                                Q.Cast(minion, true);
                        }
                    }
                }
            }

            if (config.SubMenu("Misc").Item("useRGlobal").GetValue<bool>())
            {
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsEnemy))
                {
                    if (R.CanCast(enemy) && enemy.Health <= 150)
                    {
                            var predition = R.GetPrediction(enemy, true);
                            var preditionForR = R.GetPrediction(enemy);
                            R.Cast(preditionForR.CastPosition, true);
                    }
                }
            }
            if (true) //cleanse cc
            {
                if (CCed()) W.Cast();
                if (player.HasBuffOfType(BuffType.Blind) &&
                    config.SubMenu("Misc").SubMenu("ccs").Item("blind").GetValue<bool>() &&
                    player.Distance(target.Position) <= player.AttackRange &&
                    target != null)
                    W.Cast();
            }
        }
        static bool CCed()
        {
            foreach (var effect in buffs)
            {
                if (player.HasBuffOfType(effect) &&
                    config.SubMenu("Misc").SubMenu("ccs").Item(effect.ToString().ToLower()).GetValue<bool>())
                {
                    return true;
                }
            }
            return false;
        }
    }
}
