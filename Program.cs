using System;

class Program
{
	static void Main(string[] args)
	{
		Game game = new Game();
		game.Start();
	}
}

class Game
{
	private Dictionary<string, Action<GameObject>> commands = new Dictionary<string, Action<GameObject>>();

	public static int WagnerFischer(string input, string command)
	{
		int[,] matrix = new int[input.Length + 1, command.Length + 1];
		for (int i = 0; i <= input.Length; i++)
		{
			matrix[i, 0] = i;
		}
		for (int j = 0; j <= command.Length; j++)
		{
			matrix[0, j] = j;
		}
		for (int i = 1; i <= input.Length; i++)
		{
			for (int j = 1; j <= command.Length; j++)
			{
				int cost = input[i - 1] == command[j - 1] ? 0 : 1;
				matrix[i, j] = Math.Min(matrix[i - 1, j] + 1, Math.Min(matrix[i, j - 1] + 1, matrix[i - 1, j - 1] + cost));
			}
		}
		return matrix[input.Length, command.Length];
	}

	public static string ClosestCommand(string input, List<string> commands)
	{
		string closestCommand = "";
		int closestDistance = int.MaxValue;
		foreach (string command in commands)
		{
			int distance = WagnerFischer(input, command);
			if (distance < closestDistance)
			{
				closestCommand = command;
				closestDistance = distance;
			}
		}
		return closestCommand;
	}

	public static string Capitalize(string input)
	{
		return input.First().ToString().ToUpper() + input.Substring(1);
	}

	public static void Help()
	{
		Console.WriteLine("Commands:");
		Console.WriteLine("help - shows you the list of commands");
		Console.WriteLine("inventory - lists the objects in your inventory");
		Console.WriteLine("examine [object] - examine an object");
		Console.WriteLine("equip [object] - put an object into your inventory");
		Console.WriteLine("unequip [object] - remove an object from your inventory");
		Console.WriteLine("goto [place] - go to a place");
		Console.WriteLine("attack [enemy] - attack an enemy");
		Console.WriteLine("lookaround - look around the place");
		Console.WriteLine("exit - exit the game");
	}

	public void Start()
	{
		// Print welcome message
		Console.WriteLine("Welcome to the Adventure Game!");
		Console.WriteLine("");
		Help();
		Console.WriteLine("");
		Console.Write("Press any key to start the game");
		Console.ReadKey();
		Console.Clear();

		// Create game objects
		Weapon sword = new Weapon("sword", "a sharp weapon", 10);
		Weapon axe = new Weapon("axe", "a heavy weapon", 15, false);
		Key fake_key = new Key("key", "might be for the exit");
		Key key = new Key("key", "a small key");
		HealthPack health_potion = new HealthPack("healthPotion", "a small potion that restores health", 15);
		HealthPack health_elixir = new HealthPack("healthElixir", "a pretty big jug of elixir juice that restores health", 35);
		HealthPack health_crate = new HealthPack("healthCrate", "a big crate that restores health", 50);
		Place room1 = new Place("room1", "a dark room");
		Place room2 = new Place("room2", "a mysterious room", key);
		Place room3 = new Place("room3", "a room with portals to either room1 or room2", key);
		Enemy goblin = new Enemy("goblin", "an angry goblin", 50, axe.Damage, axe);
		Enemy skeleton_knight = new Enemy("skeletonKnight", "a fearsome skeleton knight", 100, sword.Damage, key);

		// Set up game world
		// Room 1
		room1.AddObject(sword);
		room1.AddObject(axe);
		room1.AddObject(key);
		room1.AddExit(room2);
		room1.AddEnemy(goblin);
		// Room 2
		room2.AddObject(fake_key);
		room2.AddObject(health_crate);
		room2.AddExit(room1);
		room2.AddExit(room3);
		room2.AddEnemy(skeleton_knight);
		// Room 3
		room3.AddExit(room1);
		room3.AddExit(room2);

		// Start game loop
		bool gameOver = false;
		Place currentPlace = room1;
		Player player = new Player("player", 100, currentPlace);
		Console.WriteLine("You are in " + currentPlace.Name + " (" + currentPlace.Description + ")");

		while (!gameOver)
		{
			Console.Write("> ");
			string input = Console.ReadLine().ToLower();
			Console.Clear();

			string[] command = input.Split(' ');

			// Add commands
			commands.Add("help", (target) => { Help(); });
			commands.Add("inventory", (target) => { player.DisplayInventory(); });
			commands.Add("examine", currentPlace.ExamineObject);
			commands.Add("equip", player.AddToInventory);
			commands.Add("unequip", player.RemoveFromInventory);
			commands.Add("goto", (target) => { currentPlace = currentPlace.GoToPlace(command[1], player); player.CurrentPlace = currentPlace;});
			commands.Add("attack", (target) => { player.Attack((Enemy)target); if (player.Health <= 0) { gameOver = true; } });
			commands.Add("lookaround", (target) => { currentPlace.LookAround(player); });
			commands.Add("exit", (target) => { gameOver = true; });

			if (command.Length >= 1)
			{
				string action = command[0];
				string target = "";
				if (command.Length > 1)
				{
					for (int i = 1; i < command.Length; i++)
					{
						target += command[i] + " ";
					}
					target = target.Trim();
				}

				if (commands.ContainsKey(action))
				{
					GameObject obj = currentPlace.GetObject(target);
					try
					{
						commands[action](obj);
					}
					catch (System.NullReferenceException)
					{
						Console.WriteLine("Object not found!");
					}
				}
				else
				{
					Console.WriteLine("Invalid command!");
					Console.WriteLine("Did you mean: " + ClosestCommand(action, commands.Keys.ToList()) + "?");
				}
			}
			else{
				Console.WriteLine("You need to input a command!");
			}

			commands.Clear();
		}
	}
}

class GameObject
{
	public string Name { get; }
	public string Description { get; }
	public Action<Player> OnEquip { get; set; }

	public GameObject(string name, string description)
	{
		Name = name.ToLower();
		Description = description;
	}
}

class HealthPack : GameObject
{
	public int Health { get; }

	public HealthPack(string name, string description, int health) : base(name, description)
	{
		Health = health;
		OnEquip = Heal;
	}

	public void Heal(Player player)
	{
		player.Health += Health;
		Console.WriteLine("You used the health pack and gained " + Health + " health!");
		player.DisplayHealth();
		player.RemoveFromInventory(this);
	}
}

class Weapon : GameObject
{
	public int Damage { get; }
	public bool CanEquip { get; set; }

	public Weapon(string name, string description, int damage, bool canEquip = true) : base(name, description)
	{
		Damage = damage;
		CanEquip = canEquip;
	}

	public void Unlock()
	{
		Console.WriteLine("You can now equip " + Name + "!");
		CanEquip = true;
	}
}

class Key : GameObject
{
	public Key(string name, string description) : base(name, description)
	{
	}
}

class Character : GameObject
{
	public int maxHealth { get; }
	public int Health { get; set; }
	public int Damage { get; set; }

	public Character(string name, string description, int health, int damage) : base(name, description)
	{
		maxHealth = health;
		Health = health;
		Damage = damage;
	}

	public ConsoleColor FromArgb(byte r, byte g, byte b)
	{
		ConsoleColor ret = 0;
		double rr = r, gg = g, bb = b, delta = double.MaxValue;

		foreach (ConsoleColor cc in Enum.GetValues(typeof(ConsoleColor)))
		{
			var n = Enum.GetName(typeof(ConsoleColor), cc);
			var c = System.Drawing.Color.FromName(n == "DarkYellow" ? "Orange" : n);
			var t = Math.Pow(c.R - rr, 2.0) + Math.Pow(c.G - gg, 2.0) + Math.Pow(c.B - bb, 2.0);
			if (t == 0.0)
			{
				return cc;
			}
			if (t < delta)
			{
				delta = t;
				ret = cc;
			}
		}
		return ret;
	}

	public void DisplayHealth()
	{
		int health = Health;
		int healthBarSize = 20;
		double healthPercent = 0;
		if (health > 0)
		{
			healthPercent = (double)health / maxHealth;
		}
		int healthBarFill = (int)Math.Floor(healthPercent * healthBarSize);
		byte r = (byte)(255 * (1 - healthPercent));
		byte g = (byte)(255 * healthPercent);
		string healthBar = new string('█', healthBarFill) + new string(' ', healthBarSize - healthBarFill);
		Console.Write(Name + "'s health: ");
		Console.ForegroundColor = FromArgb(r, g, 0);
		Console.WriteLine(healthBar + " " + health + "/" + maxHealth);
		Console.ResetColor();
	}

	public void Attack(Character target)
	{
		Console.WriteLine(Name + " attacks " + target.Name + "!");
		target.Health -= Damage;
		target.DisplayHealth();
	}
}

class Player : Character
{
	public List<GameObject> Inventory { get; }
	public Place CurrentPlace { get; set; }
	public int InventorySize { get; set; }
	public Weapon EquippedWeapon { get; set; }

	public Player(string name, int health, Place currentPlace, int inventorySize = 5) : base(name, "the player", health, 0)
	{
		Inventory = new List<GameObject>();
		CurrentPlace = currentPlace;
		InventorySize = inventorySize;
	}

	public void UpgradeInventory(int quantity)
	{
		Console.WriteLine("Inventory size increased by " + quantity + " slots!");
		InventorySize += quantity;
	}

	public void AddToInventory(GameObject obj)
	{
		if (Inventory.Count >= InventorySize)
		{
			Console.WriteLine("Your inventory is full!");
			return;
		}
		else if (Inventory.Contains(obj))
		{
			Console.WriteLine("You already have this object in your inventory!");
			return;
		}
		else if (obj is Weapon)
		{
			if (EquippedWeapon != null)
			{
				Console.WriteLine("You can't equip another weapon while you have one equipped!");
				return;
			}
			else
			{
				EquippedWeapon = (Weapon)obj;
				Damage = EquippedWeapon.Damage;
			}
		}
		else if (obj is Character)
		{
			Console.Write("You can't pick up a");
			if (obj is Enemy)
			{
				Console.WriteLine("n enemy!");
			}
			else
			{
				Console.WriteLine(" character!");
			}
			return;
		}
		else if (CurrentPlace.GetObject(obj.Name) == null)
		{
			Console.WriteLine("You can't pick up an object that's not in the same place as you!");
			return;
		}
		Console.WriteLine("You equip " + obj.Name + " into your inventory!");
		if (obj.OnEquip != null)
		{
			obj.OnEquip(this);
		}
		Inventory.Add(obj);
	}

	public void RemoveFromInventory(GameObject obj)
	{
		if (!Inventory.Contains(obj))
		{
			Console.WriteLine("You don't have this object in your inventory!");
			return;
		}
		else if (obj is Weapon)
		{
			EquippedWeapon = null;
			Damage = 0;
		}
		Console.WriteLine("You remove " + obj.Name + " from your inventory!");
		Inventory.Remove(obj);
	}

	public void DisplayInventory()
	{
		Console.WriteLine("Inventory:");
		foreach (GameObject obj in Inventory)
		{
			Console.WriteLine(obj.Name);
		}
	}

	public void Attack(Enemy enemy)
	{
		if (EquippedWeapon == null)
		{
			Console.WriteLine("You need to equip a weapon to attack!");
			return;
		}
		bool fightOver = false;
		while (!fightOver)
		{
			base.Attack(enemy);
			if (enemy.Health <= 0)
			{
				Console.WriteLine("You defeated " + enemy.Name + "!");
				enemy.OnDeath(this);
				fightOver = true;
			}
			else
			{
				enemy.Attack(this);
				if (Health <= 0)
				{
					Console.WriteLine("You were defeated by " + enemy.Name + "!");
					fightOver = true;
				}
			}
		}
	}
}

class Enemy : Character
{
	private int GetLevel()
	{
		return (int)Math.Floor(((float)Damage * .1) + ((float)Health * .01));
	}

	public string Description { get; }
	public GameObject Drop { get; }
	public int Level { get { return GetLevel(); } }

	public Enemy(string name, string description, int health, int damage, GameObject drop) : base(name, description, health, damage)
	{
		Drop = drop;
	}

	public void OnDeath(Player player)
	{
		player.UpgradeInventory(Level);
		if (Drop is Weapon)
		{
			((Weapon)Drop).Unlock();
		}
		else
		{
			player.Inventory.Add(Drop);
			Console.WriteLine("You picked up " + Drop.Name + "!");
		}
	}
}

class Place
{
	public string Name { get; }
	public string Description { get; }
	public Key Key { get; }
	private List<GameObject> objects;
	private List<Enemy> enemies;
	private List<Place> exits;

	public Place(string name, string description, Key key = null)
	{
		Name = name;
		Description = description;
		Key = key;
		objects = new List<GameObject>();
		enemies = new List<Enemy>();
		exits = new List<Place>();
	}

	public GameObject GetObject(string name)
	{
		GameObject obj = objects.Find(o => o.Name == name);
		if (obj == null)
		{
			obj = enemies.Find(e => e.Name == name);
		}
		return obj;
	}

	public void AddObject(GameObject obj)
	{
		objects.Add(obj);
	}

	public void AddEnemy(Enemy enemy)
	{
		enemies.Add(enemy);
	}

	public void AddExit(Place exitPlace)
	{
		exits.Add(exitPlace);
	}

	public void ExamineObject(GameObject obj)
	{
		GameObject objectToExamine = objects.Find(o => o == obj);
		if (objectToExamine != null)
		{
			Console.WriteLine(objectToExamine.Description);
		}
		else
		{
			Console.WriteLine("Object not found!");
		}
	}

	public void CheckEnemies()
	{
		for (int i = 0; i < enemies.Count; i++)
		{
			if (enemies[i].Health <= 0)
			{
				enemies.RemoveAt(i);
			}
		}
	}

	public Place GoToPlace(string exitName, Player player)
	{
		CheckEnemies();
		Place exit = exits.Find(e => e.Name == exitName);
		if (exit != null)
		{
			if (exit.Key != null && !player.Inventory.Contains(exit.Key))
			{
				Console.WriteLine("You need the proper key to unlock this exit!");
				return this;
			}
			else if (enemies.Count > 0)
			{
				Console.WriteLine("You can't leave while there are enemies in the room!");
				return this;
			}
			Console.WriteLine("You go to " + exit.Name + " (" + exit.Description + ")");
			player.RemoveFromInventory(exit.Key);
			return exit;
		}
		else
		{
			Console.WriteLine("Exit not found!");
			return this;
		}
	}

	public void LookAround(Player player)
	{
		CheckEnemies();
		Console.WriteLine("You look around and you see:");
		foreach (GameObject obj in objects)
		{
			if (player.Inventory.Contains(obj))
			{
				continue;
			}
			ConsoleColor color = ConsoleColor.White;
			if (obj is HealthPack)
			{
				color = ConsoleColor.Green;
			}
			else if (obj is Weapon)
			{
				if (!((Weapon)obj).CanEquip)
				{
					continue;
				}
				color = ConsoleColor.Blue;
			}
			else if (obj is Key)
			{
				color = ConsoleColor.Yellow;
			}
			Console.Write(" * ");
			Console.ForegroundColor = color;
			Console.WriteLine(obj.Name);
			Console.ResetColor();
		}
		foreach (Enemy enemy in enemies)
		{
			Console.Write(" * ");
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(enemy.Name);
			Console.ResetColor();
		}
		foreach (Place exit in exits)
		{
			if (exit == this)
			{
				continue;
			}
			Console.Write(" * ");
			Console.ForegroundColor = ConsoleColor.DarkYellow;
			Console.WriteLine(exit.Name);
			Console.ResetColor();
		}
	}
}
// TODO: add a more dynamic and scalable system for adding game objects (read from json file)
// TODO: add more objects, enemies, & places