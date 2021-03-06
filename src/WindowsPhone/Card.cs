using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace Spider
{
	internal enum Suit
	{
		Spade,
		Diamond,
		Club,
		Heart,
	}

	internal enum Value
	{
		Ace,
		Two,
		Three,
		Four,
		Five,
		Six,
		Seven,
		Eight,
		Nine,
		Ten,
		Jack,
		Queen,
		King,
	}

	// TODO:Later: Refactor these into a better grouping of classes
	internal class CardResources
	{
		//public static string[] valueNames = { @"Ace", @"Two", @"Three", @"Four", @"Five", @"Six", @"Seven", @"Eight", @"Nine", @"Ten", @"Jack", @"Queen", @"King" };
		private static readonly string[] ValueNamesShort =
		{
			@"A", @"2", @"3", @"4", @"5", @"6", @"7", @"8", @"9", @"10", @"J", @"Q",
			@"K"
		};

		//public static string[] suitNames = { @"Spades", @"Diamonds", @"Clubs", @"Hearts" };
		private static readonly string[] SuitNamesShort = {@"Spade", @"Diamond", @"Club", @"Heart"};

		public static Texture2D CardTex { get; private set; }
		public static Texture2D CardBackTex { get; private set; }
		public static Texture2D HighlightEndTex { get; private set; }
		public static Texture2D HightlightCenterTex { get; private set; }
		public static Texture2D PlaceholderTex { get; private set; }
		public static List<Texture2D> SuitTex { get; private set; }
		public static List<Texture2D> ValueTex { get; private set; }
		public static Texture2D GradientTex { get; private set; }
		public static Texture2D BlankTex { get; private set; }

		public static Texture2D UndoTex { get; private set; }

		public static SpriteFont WinFont { get; private set; }
		public static SpriteFont AgainFont { get; private set; }
		public static Texture2D RocketTex { get; private set; }
		public static List<Texture2D> PuffTex { get; private set; }
		public static List<Texture2D> FireworkParticleTex { get; private set; }

		public static int ContentCount()
		{
			return 1 + 1 + 3 + SuitNamesShort.Length + ValueNamesShort.Length + 8 + 2;
		}

		public static void LoadResources(GraphicsDevice graphicsDevice, ContentManager content,
			ContentLoadNotificationDelegate callback)
		{
			LoadCardResources(graphicsDevice, content, callback);
			LoadBoardResources(graphicsDevice, content, callback);
			LoadVictoryResources(graphicsDevice, content, callback);
		}

		private static Texture2D LoadThemePackResource(ContentManager content, string resource)
		{
			return content.Load<Texture2D>(@"ThemePacks\" + Options.ThemePack + @"\" + resource);
		}

		public static void LoadCardResources(GraphicsDevice graphicsDevice, ContentManager content,
			ContentLoadNotificationDelegate callback)
		{
			CardTex = LoadThemePackResource(content, @"Card\Card");
			callback();

			CardBackTex = LoadThemePackResource(content, @"Card\CardBack_White");
			callback();

			HighlightEndTex = LoadThemePackResource(content, @"Card\Highlight_End");
			callback();
			HightlightCenterTex = LoadThemePackResource(content, @"Card\Highlight_Center");
			callback();
			PlaceholderTex = LoadThemePackResource(content, @"Card\Placeholder");
			callback();

			var suitTex = new List<Texture2D>(SuitNamesShort.Length);
			foreach (string suit in SuitNamesShort)
			{
				suitTex.Add(LoadThemePackResource(content, @"Card\" + suit));
				callback();
			}
			SuitTex = suitTex;

			var valueTex = new List<Texture2D>(ValueNamesShort.Length);
			foreach (string value in ValueNamesShort)
			{
				valueTex.Add(LoadThemePackResource(content, @"Card\" + value));
				callback();
			}
			ValueTex = valueTex;
		}

		public static void LoadBoardResources(GraphicsDevice graphicsDevice, ContentManager content, ContentLoadNotificationDelegate callback)
		{
			GradientTex = content.Load<Texture2D>("Gradient");
			callback();
			BlankTex = new Texture2D(graphicsDevice, 1, 1, false, SurfaceFormat.Color);
			BlankTex.SetData<Color>(new Color[] {Color.White});
			callback();

			UndoTex = LoadThemePackResource(content, "Undo");
			callback();
		}

		public static void LoadVictoryResources(GraphicsDevice graphicsDevice, ContentManager content, ContentLoadNotificationDelegate callback)
		{
			WinFont = content.Load<SpriteFont>(@"Win\WinFont");
			callback();
			AgainFont = content.Load<SpriteFont>(@"Win\AgainFont");
			callback();
			RocketTex = content.Load<Texture2D>(@"Win\Rocket");
			callback();
			var puffTex = new List<Texture2D>();
			for (int i = 0; i < 2; i++)
			{
				puffTex.Add(content.Load<Texture2D>(@"Win\Puff" + (i + 1)));
				callback();
			}
			PuffTex = puffTex;
			var particleTex = new List<Texture2D>();
			for (int i = 0; i < 2; i++)
			{
				particleTex.Add(content.Load<Texture2D>(@"Win\Firework" + (i + 1)));
				callback();
			}
			FireworkParticleTex = particleTex;
		}
	}

	internal class Card
	{
		public Suit Suit { get; private set; }
		public Value Value { get; private set; }
		public bool Visible { get; set; }

		public CardView View { get; set; }

		public double RandomSeed { get; private set; } // For shuffling purposes

		public Card(Suit suit, Value value, Random random)
		{
			Suit = suit;
			Value = value;

			RandomSeed = random.NextDouble();
		}

		public Card(Card cardOrig)
		{
			Suit = cardOrig.Suit;
			Value = cardOrig.Value;
			Visible = cardOrig.Visible;
		}

		public void Reveal()
		{
			Visible = true;
		}
	}
}