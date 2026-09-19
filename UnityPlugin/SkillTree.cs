using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;

namespace Archskipelagill;

public static partial class SkillTree {
    public enum SkillNodeType { UNKNOWN, STAT, CHEST, PERK, BOSS, BOSS_FINAL };
    public enum SkillNodeSpawnId { NONE, MAGE, STRONGMAN, FOX, PROTOTYPE, DWARVES, DRAGON };
    public enum SkillNodeRegion { MAGE, PROTOTYPE, DRAGON, STRONGMAN, FOX, DWARVES, BOSSES };

    public struct SkillNode(SkillNodeType _type, int[] _neighbors, SkillNodeSpawnId _spawnId, SkillNodeRegion _region, int _originalIndex, int _chestIndex, int _perkIndex) {
        public SkillNodeType type = _type;
        public int[] neighbors = _neighbors;
        public SkillNodeSpawnId spawnId = _spawnId;
        public SkillNodeRegion region = _region;
        public int originalIndex = _originalIndex;
        public int chestIndex = _chestIndex;
        public int perkIndex = _perkIndex;
    }

    static readonly string[] SPAWN_TARGET_NAMES = [
        "Mage",
        "Baldo",
        "Fox",
        "Nain",
        "Jugger",
        "Dragon"
    ];

    static int FindNodeDistance(List<skigillNode> allValidNodes, skigillNode node1, skigillNode node2) {
        var ind1 = allValidNodes.IndexOf(node1);
        var ind2 = allValidNodes.IndexOf(node2);

        var finalBossNode = allValidNodes.Find(n => n.adjacent.Count == 0 && n.type == 22);

        var dist = new int[allValidNodes.Count()];
        Array.Fill(dist, -1);
        Queue<int> q = new();
        dist[ind1] = 0;
        q.Enqueue(ind1);
        while(q.Count > 0) {
            var indHere = q.Dequeue();
            if(indHere == ind2)
                return dist[ind2];
            var adj = allValidNodes[indHere].adjacent.ToList();
            if(allValidNodes[indHere].type == 22) adj.Add(finalBossNode.gameObject);
            foreach(var nxgo in adj) {
                var nxsn = nxgo.GetComponent<skigillNode>();
                var indNext = allValidNodes.IndexOf(nxsn);
                if(dist[indNext] == -1) {
                    dist[indNext] = dist[indHere] + 1;
                    q.Enqueue(indNext);
                }
            }
        }

        return -1;
    }

    public static void ScrapeSkillTree() {
        var gridObj = UnityEngine.GameObject.Find("gridHolder/grid").transform;
        var avnUnsorted = GameObject.FindObjectsByType<skigillNode>(FindObjectsSortMode.InstanceID).Where(n => n.isActiveAndEnabled && !n.metaProg && n.transform.IsChildOf(gridObj)).ToList();
        var allValidNodes = avnUnsorted.OrderBy(n => n.transform.position.y).ThenBy(n => n.transform.position.x).ToList();

        List<string> outputPy = [];
        List<string> outputCs = [];

        var finalBossNode = allValidNodes.Find(n => n.adjacent.Count == 0 && n.type == 22);

        int chestCount = 0;
        int perkCount = 0;

        for(var i = 0; i < allValidNodes.Count; i++) {
            var node = allValidNodes[i];
            var skillNodeType = node.type switch {
                0 => "CHEST",
                20 => "PERK",
                22 => "BOSS",
                _ => "STAT"
            };
            int chestIndex = -1;
            if(skillNodeType == "CHEST") {
                chestIndex = chestCount;
                chestCount++;
            }
            int perkIndex = -1;
            if(skillNodeType == "PERK") {
                perkIndex = perkCount;
                perkCount++;
            }
            var connexList = node.adjacent.Select(n => allValidNodes.IndexOf(n.GetComponent<skigillNode>())).ToList();
            if(node == finalBossNode) {
                skillNodeType = "BOSS_FINAL";
            } else if(skillNodeType == "BOSS") {
                connexList.Add(allValidNodes.IndexOf(finalBossNode));
            }
            var spawnId = node.name switch {
                "Mage" => "MAGE",
                "Baldo" => "STRONGMAN",
                "Fox" => "FOX",
                "Dragon" => "DRAGON",
                "Jugger" => "PROTOTYPE",
                "Nain" => "DWARVES",
                _ => "NONE"
            };
            var spawnDistances = new List<int>();
            for(var j = 0; j < SPAWN_TARGET_NAMES.Length; j++) {
                var targetNode = GameObject.Find("gridHolder/grid/Perks/" + SPAWN_TARGET_NAMES[j]).GetComponent<skigillNode>();
                spawnDistances.Add(FindNodeDistance(allValidNodes, targetNode, node));
            }
            spawnDistances.Add(FindNodeDistance(allValidNodes, node, finalBossNode));
            var closestDist = spawnDistances.Where(n => n >= 0).Min();
            var regions = spawnDistances.Select((d, i) => (d, i)).Where(n => n.d == closestDist).Select(n => n.i);
            var highestRegion = regions.OrderBy(n => n).Last();

            outputPy.Add($"\tSkillNode(SkillNodeType.{skillNodeType}, [{string.Join(", ", connexList)}], SkillNodeSpawnId.{spawnId}, SkillNodeRegion.{Enum.GetName(typeof(SkillNodeRegion), highestRegion)}, {avnUnsorted.IndexOf(node)}, {chestIndex}, {perkIndex})");
            outputCs.Add($"\t\tnew SkillNode(SkillNodeType.{skillNodeType}, [{string.Join(", ", connexList)}], SkillNodeSpawnId.{spawnId}, SkillNodeRegion.{Enum.GetName(typeof(SkillNodeRegion), highestRegion)}, {avnUnsorted.IndexOf(node)}, {chestIndex}, {perkIndex})");
        }
        var dir = Directory.GetCurrentDirectory();
        File.WriteAllText(Path.Join(dir, "skilltree_data.py"),
            $$"""
            from .skilltree import SkillNodeType, SkillNodeSpawnId, SkillNodeRegion, SkillNode
            from enum import Enum

            SKILL_TREE = {
            {{string.Join("," + System.Environment.NewLine, outputPy)}}
            }
            """);
        File.WriteAllText(Path.Join(dir, "SkillTreeData.cs"),
            $$"""
            using System.Collections.Generic;

            namespace Archskipelagill;

            public static partial class SkillTree {
                public static List<SkillNode> skillTree = [
                {{string.Join("," + System.Environment.NewLine, outputCs)}}
                ];
            }
            """);
    }
}

//perkid = 0 for non-perks
//type 0 = chests
//type 1 = STR
//type 2 = DEX
//type 3 = INT
//type 4 = regen
//type 5 = armor
//type 6 = evade
//type 7 = range
//type 8 = crit
//type 9 = compspeed/ATKSPD
//type 10 = fire
//type 11 = thunder
//type 12 = ice
//type 13 = magnet
//type 14 = luck
//type 15 = speed
//type 16 = stun
//type 17 = poison
//type 18 = flat
//type 19 = title screen??
//type 20 = perk or character/weapon unlock
//type 21 = meta money
//type 22 = boss