using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using System.Reflection;
using System.Text;
using Spider.Resources;

namespace Spider
{
	internal delegate void OnClick();

	internal delegate void OnButtonClicked(MenuButton button);

	internal class Menu
	{
		private static Texture2D spiderCardTex;
		private static Rectangle spiderCardBounds;

		private static SpriteFont menuFont;
		private static SpriteFont menuSubFont;
		private static SpriteFont menuTrialDetailFont;
		private static SpriteFont menuBackgroundFont;

		private static Texture2D oneSuitTex;
		private static Texture2D twoSuitTex;
		private static Texture2D fourSuitTex;

		private static Texture2D trialBannerTex;

		public static int ContentCount()
		{
			return 9 + StatisticsView.ContentCount() + OptionsView.ContentCount() + AboutView.ContentCount();
		}

		public static void LoadContent(ContentManager content, ContentLoadNotificationDelegate callback)
		{
			spiderCardTex = content.Load<Texture2D>(@"Menu\SpiderCard");
			callback();
			menuFont = content.Load<SpriteFont>(@"Menu\MainMenuFont");
			callback();
			menuSubFont = content.Load<SpriteFont>(@"Menu\MenuSubFont");
			callback();
			menuTrialDetailFont = content.Load<SpriteFont>(@"Menu\MenuTrialDetailFont");
			callback();
			menuBackgroundFont = content.Load<SpriteFont>(@"Menu\MenuBackgroundFont");
			callback();

			oneSuitTex = content.Load<Texture2D>(@"Menu\OneSuit");
			callback();
			twoSuitTex = content.Load<Texture2D>(@"Menu\TwoSuits");
			callback();
			fourSuitTex = content.Load<Texture2D>(@"Menu\FourSuits");
			callback();

			if (GameStateManager.IsTrial)
				trialBannerTex = content.Load<Texture2D>(@"Menu\TrialBanner");
			callback();

			StatisticsView.LoadContent(content, callback);
			OptionsView.LoadContent(content, callback);
			AboutView.LoadContent(content, callback);
		}

		private Rectangle viewRect;
		private List<MenuButton> buttons = new List<MenuButton>();
		private List<MenuButtonAnimation> animations = new List<MenuButtonAnimation>();

		private DateTime startTime;
		private DateTime currentTime;

		private MenuSubView activeSubView;
		private TrialWindow trialWindow;

		private const int BackgroundTextScrollTime = 90;

		public Menu(Rectangle rc)
		{
			if (GameStateManager.IsTrial)
				Statistics.Load();

			viewRect = rc;

			Color switchedColor = (GameStateManager.IsTrial ? Color.DimGray : Color.White);

			float aspectRatio = (float) spiderCardTex.Bounds.Width/(float) spiderCardTex.Bounds.Height;
			int spiderCardHeight = Math.Min(GameStateManager.ViewRect.Height - 80,
				(int) ((GameStateManager.ViewRect.Width/2 - 80)/aspectRatio));
			spiderCardBounds = new Rectangle(40, (GameStateManager.ViewRect.Height - spiderCardHeight)/2,
				(int) (spiderCardHeight*aspectRatio), spiderCardHeight);

			int x = viewRect.Width/2 - 40;
			int y = (int) (viewRect.Height*0.06);
			int xSpacing = (int) (viewRect.Width/8);
			int ySpacing = (int) (viewRect.Height*0.13);
			int xImgSpacing = (int) (viewRect.Width*0.03);

			TextMenuButton newGameButton = new TextMenuButton() {Text = Strings.NewGame, Font = menuFont};
			Vector2 newGameSize = newGameButton.Font.MeasureString(newGameButton.Text);
			newGameButton.Rect = new Rectangle(x, y, (int) newGameSize.X, newGameButton.Font.LineSpacing);
			newGameButton.ButtonClickDelegate = OnNewGameClicked;
			buttons.Add(newGameButton);

			int imageWidth = GameStateManager.ViewRect.Width/8 + 20;
			int imageHeight = imageWidth*oneSuitTex.Height/oneSuitTex.Width;

			ImageMenuButton oneSuitButton = new ImageMenuButton() {Texture = oneSuitTex, Visible = false, Enabled = false};
			oneSuitButton.Rect = new Rectangle(newGameButton.Rect.X, newGameButton.Rect.Bottom + 10, imageWidth, imageHeight);
			oneSuitButton.ButtonClickDelegate = OnSuitImageClicked;
			buttons.Add(oneSuitButton);

			ImageMenuButton twoSuitButton = new ImageMenuButton()
			{
				Texture = twoSuitTex,
				Visible = false,
				Enabled = false,
				Color = switchedColor
			};
			twoSuitButton.Rect = new Rectangle(newGameButton.Rect.X + imageWidth + xImgSpacing, newGameButton.Rect.Bottom + 10,
				imageWidth, imageHeight);
			twoSuitButton.ButtonClickDelegate = OnSuitImageClicked;
			buttons.Add(twoSuitButton);

			ImageMenuButton fourSuitButton = new ImageMenuButton()
			{
				Texture = fourSuitTex,
				Visible = false,
				Enabled = false,
				Color = switchedColor
			};
			fourSuitButton.Rect = new Rectangle(newGameButton.Rect.X + imageWidth*2 + xImgSpacing*2,
				newGameButton.Rect.Bottom + 10, imageWidth, imageHeight);
			fourSuitButton.ButtonClickDelegate = OnSuitImageClicked;
			buttons.Add(fourSuitButton);

			bool resume = Board.ResumeGameExists();
			TextMenuButton resumeButton = new TextMenuButton() {Text = Strings.Resume, Font = menuFont, Enabled = resume};
			if (!resume)
				resumeButton.Color = new Color(64, 64, 64);
			Vector2 resumeSize = resumeButton.Font.MeasureString(resumeButton.Text);
			resumeButton.Rect = new Rectangle(x, y + ySpacing*2, (int) resumeSize.X, resumeButton.Font.LineSpacing);
			resumeButton.ButtonClickDelegate = OnResumeClicked;
			buttons.Add(resumeButton);

			TextMenuButton optionsButton = new TextMenuButton() {Text = Strings.Options, Font = menuSubFont};
			Vector2 optionsSize = optionsButton.Font.MeasureString(optionsButton.Text);
			optionsButton.Rect = new Rectangle(x, y + ySpacing*4, (int) optionsSize.X, optionsButton.Font.LineSpacing);
			optionsButton.ButtonClickDelegate = OnOptionsClicked;
			buttons.Add(optionsButton);

			TextMenuButton statsButton = new TextMenuButton()
			{
				Text = Strings.Statistics,
				Font = menuSubFont,
				Color = switchedColor
			};
			Vector2 statsSize = statsButton.Font.MeasureString(statsButton.Text);
			statsButton.Rect = new Rectangle(x, y + ySpacing*5, (int) statsSize.X, statsButton.Font.LineSpacing);
			statsButton.ButtonClickDelegate = OnStatisticsClicked;
			buttons.Add(statsButton);

			TextMenuButton aboutButton = new TextMenuButton() {Text = Strings.About, Font = menuSubFont};
			Vector2 aboutSize = aboutButton.Font.MeasureString(aboutButton.Text);
			aboutButton.Rect = new Rectangle(x, y + ySpacing*6, (int) aboutSize.X, aboutButton.Font.LineSpacing);
			aboutButton.ButtonClickDelegate = OnAboutClicked;
			buttons.Add(aboutButton);

			if (GameStateManager.IsTrial)
			{
				int bannerSize = (int) (viewRect.Height*0.45);
				ImageMenuButton trialBannerButton = new ImageMenuButton() {Texture = trialBannerTex};
				trialBannerButton.Rect = new Rectangle(viewRect.Right - bannerSize, viewRect.Bottom - bannerSize, bannerSize,
					bannerSize);
				trialBannerButton.ButtonClickDelegate = OnTrialBannerClicked;
				buttons.Add(trialBannerButton);

				TextMenuButton bannerTextButton = new TextMenuButton()
				{
					Text = Strings.Menu_TrialBanner,
					Font = menuSubFont,
					Color = Color.Black,
					Rotation = (float) (-Math.PI/4)
				};
				Vector2 bannerTextSize = bannerTextButton.Font.MeasureString(bannerTextButton.Text);
				bannerTextButton.Rect = new Rectangle(trialBannerButton.Rect.X + 3*(int) bannerTextSize.Y/4,
					viewRect.Bottom - 3*(int) bannerTextSize.Y/4, (int) bannerTextSize.X, (int) bannerTextSize.Y);
				buttons.Add(bannerTextButton);
			}

			startTime = DateTime.Now;
			currentTime = DateTime.Now;
		}

		public void Update(Game game)
		{
			if (trialWindow != null)
			{
				if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
				{
					trialWindow = null;
					return;
				}

				if (trialWindow.Update() == false)
				{
					CheckTrialStatus();
					trialWindow = null;
					return;
				}
			}
			else if (activeSubView != null)
			{
				if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
				{
					activeSubView.OnClose();
					activeSubView = null;
					return;
				}

				activeSubView.Update();
			}
			else
			{
				if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
					game.Exit();

				currentTime = DateTime.Now;

				foreach (TouchLocation touchLoc in TouchPanel.GetState())
				{
					Point pt = new Point((int) touchLoc.Position.X, (int) touchLoc.Position.Y);

					if (touchLoc.State == TouchLocationState.Released)
					{
						foreach (MenuButton button in buttons)
						{
							if (button.Rect.Contains(pt) && button.Enabled)
							{
								if (button.ButtonClickDelegate != null)
									button.ButtonClickDelegate(button);
								break;
							}
						}
					}
				}

				List<MenuButtonAnimation> finishedAnimations = new List<MenuButtonAnimation>();
				foreach (MenuButtonAnimation animation in animations)
				{
					if (!animation.Update())
						finishedAnimations.Add(animation);
				}
				foreach (MenuButtonAnimation animation in finishedAnimations)
					animations.Remove(animation);
			}
		}

		public void Render(Rectangle rect, SpriteBatch batch)
		{
			if (activeSubView != null)
			{
				activeSubView.Render(rect, batch);
			}
			else
			{
				TimeSpan elapsed = currentTime - startTime;
				float scale = 3.0f;
				Vector2 backgroundSize = menuBackgroundFont.MeasureString(Strings.AppName)*scale;
				float x = (float) (-backgroundSize.X*(elapsed.TotalSeconds/BackgroundTextScrollTime));
				float y = -(backgroundSize.Y - GameStateManager.ViewRect.Height)/2;
				Vector2 offset = new Vector2(x, y);

				batch.Begin();
				batch.DrawString(menuBackgroundFont, Strings.AppName, offset, new Color(32, 32, 32), 0.0f, Vector2.Zero, scale,
					SpriteEffects.None, 1);
				if (offset.X + backgroundSize.X < GameStateManager.ViewRect.Width)
				{
					offset.X += backgroundSize.X;
					batch.DrawString(menuBackgroundFont, Strings.AppName, offset, new Color(32, 32, 32), 0.0f, Vector2.Zero, scale,
						SpriteEffects.None, 1);
				}

				if (spiderCardTex != null)
				{
					batch.Draw(spiderCardTex, spiderCardBounds, Color.White);
				}
				else
				{
					// This is bad, but we're seeing crashes in the wild that look like they're hitting this
				}

				foreach (MenuButton button in buttons)
				{
					if (button.Visible)
					{
						TextMenuButton textButton = button as TextMenuButton;
						ImageMenuButton imageButton = button as ImageMenuButton;
						if (textButton != null)
						{
							Vector2 pos = new Vector2(button.Rect.X, button.Rect.Y);
							batch.DrawString(textButton.Font, textButton.Text, pos, button.Color, textButton.Rotation, Vector2.Zero, 1.0f,
								SpriteEffects.None, 0);
						}
						if (imageButton != null && imageButton.Texture != null)
						{
							batch.Draw(imageButton.Texture, button.Rect, button.Color);
						}
					}
				}

				batch.End();
			}

			if (trialWindow != null)
			{
				trialWindow.Render(rect, batch);
			}
		}

		private void OnNewGameClicked(MenuButton button)
		{
			MenuButton oneSuitButton = null;
			MenuButton twoSuitButton = null;
			MenuButton fourSuitButton = null;
			foreach (MenuButton menuButton in buttons)
			{
				ImageMenuButton imageButton = menuButton as ImageMenuButton;
				if (imageButton != null)
				{
					if (imageButton.Texture == oneSuitTex)
						oneSuitButton = menuButton;
					else if (imageButton.Texture == twoSuitTex)
						twoSuitButton = menuButton;
					else if (imageButton.Texture == fourSuitTex)
						fourSuitButton = menuButton;
				}
			}

			if (!oneSuitButton.Visible)
				animations.Add(new MenuButtonAnimation(oneSuitButton, oneSuitButton.Rect.Location, oneSuitButton.Rect.Location, 0.0f,
					1.0f));
			if (!twoSuitButton.Visible)
				animations.Add(new MenuButtonAnimation(twoSuitButton, twoSuitButton.Rect.Location, twoSuitButton.Rect.Location, 0.0f,
					1.0f));
			if (!fourSuitButton.Visible)
				animations.Add(new MenuButtonAnimation(fourSuitButton, fourSuitButton.Rect.Location, fourSuitButton.Rect.Location,
					0.0f, 1.0f));
		}

		private void OnSuitImageClicked(MenuButton button)
		{
			ImageMenuButton imageButton = button as ImageMenuButton;
			if (imageButton.Texture == oneSuitTex)
				Board.SuitCount = 1;
			else if (!GameStateManager.IsTrial)
			{
				if (imageButton.Texture == twoSuitTex)
					Board.SuitCount = 2;
				else if (imageButton.Texture == fourSuitTex)
					Board.SuitCount = 4;
			}
			else
			{
				trialWindow = new TrialWindow(viewRect, Strings.DisabledInTrial);
				return;
			}

			Analytics.RegisterEvent(Analytics.EventType.NewGame, Board.SuitCount);
			GameStateManager.ChangeGameState(GameState.Playing);
		}

		private void OnResumeClicked(MenuButton button)
		{
			Analytics.RegisterEvent(Analytics.EventType.ResumeGame);
			GameStateManager.ChangeGameState(GameState.Playing, true /*resume*/);
		}

		private void OnOptionsClicked(MenuButton button)
		{
			Analytics.RegisterEvent(Analytics.EventType.ViewOptions);
			activeSubView = new OptionsView(viewRect);
		}

		private void OnStatisticsClicked(MenuButton button)
		{
			Analytics.RegisterEvent(Analytics.EventType.ViewStatistics);
			if (!GameStateManager.IsTrial)
				activeSubView = new StatisticsView(viewRect);
			else
				trialWindow = new TrialWindow(viewRect, Strings.DisabledInTrial);
		}

		private void OnAboutClicked(MenuButton button)
		{
			Analytics.RegisterEvent(Analytics.EventType.ViewAbout);
			activeSubView = new AboutView(viewRect);
		}

		private void OnTrialBannerClicked(MenuButton button)
		{
			trialWindow = new TrialWindow(viewRect, Strings.Menu_TrialBannerNav);
		}

		// TODO: Probably get rid of this
		private void CheckTrialStatus()
		{
			GameStateManager.RefreshTrialStatus();
			if (!GameStateManager.IsTrial)
			{
				MenuButton twoSuitButton = null;
				MenuButton fourSuitButton = null;
				foreach (MenuButton menuButton in buttons)
				{
					ImageMenuButton imageButton = menuButton as ImageMenuButton;
					if (imageButton != null)
					{
						if (imageButton.Texture == twoSuitTex)
							twoSuitButton = menuButton;
						else if (imageButton.Texture == fourSuitTex)
							fourSuitButton = menuButton;
					}
				}

				twoSuitButton.Color = Color.White;
				fourSuitButton.Color = Color.White;
			}
		}
	}

	internal class MenuButton
	{
		public Rectangle Rect { get; set; }
		public Color Color { get; set; }
		public bool Visible { get; set; }
		public bool Enabled { get; set; }
		public OnButtonClicked ButtonClickDelegate { get; set; }

		public MenuButton()
		{
			Color = Color.White;
			Visible = true;
			Enabled = true;
		}
	}

	internal class TextMenuButton : MenuButton
	{
		public string Text { get; set; }
		public SpriteFont Font { get; set; }
		public float Rotation { get; set; }

		public void CenterTextInRectangle(Rectangle rectCenter)
		{
			Vector2 size = Font.MeasureString(Text);
			int offset = (rectCenter.Width - (int) size.X)/2;
			Rect = new Rectangle(rectCenter.X + offset, rectCenter.Y, (int)size.X, (int)size.Y);
		}
	}

	internal class ImageMenuButton : MenuButton
	{
		public Texture2D Texture { get; set; }
	}

	internal class CustomMenuButton : MenuButton
	{
		private readonly Action<MenuButton, SpriteBatch, Rectangle> _render;

		public CustomMenuButton(Action<MenuButton, SpriteBatch, Rectangle> render)
		{
			_render = render;
		}
		public void Render(SpriteBatch batch)
		{
			if (_render != null)
				_render(this, batch, Rect);
		}
	}

	internal class MenuButtonAnimation
	{
		private const double duration = 0.3;

		private MenuButton button;
		private Color endColor;
		private Point ptStart;
		private Point ptEnd;
		private float alphaStart;
		private float alphaEnd;
		private DateTime timeStart;
		private DateTime timeEnd;

		public MenuButtonAnimation(MenuButton button, Point ptStart, Point ptEnd, float alphaStart, float alphaEnd)
		{
			this.button = button;
			this.endColor = button.Color;
			this.ptStart = ptStart;
			this.ptEnd = ptEnd;
			this.alphaStart = alphaStart;
			this.alphaEnd = alphaEnd;
			this.timeStart = DateTime.Now;
			this.timeEnd = timeStart.AddSeconds(duration);

			button.Visible = true;
		}

		public bool Update()
		{
			double t = (DateTime.Now - timeStart).TotalSeconds/duration;
			button.Rect = new Rectangle(
				(int) ((ptEnd.X - ptStart.X)*t) + ptStart.X,
				(int) ((ptEnd.Y - ptStart.Y)*t) + ptStart.Y,
				button.Rect.Width,
				button.Rect.Height);
			button.Color = Color.Multiply(endColor, (float) ((alphaEnd - alphaStart)*t) + alphaStart);

			if (DateTime.Now > timeEnd)
			{
				button.Enabled = true;
				button.Color = endColor;
				return false;
			}
			return true;
		}
	}

	internal interface MenuSubView
	{
		void Update();
		void Render(Rectangle rect, SpriteBatch batch);
		void OnClose();
	}

	internal class StatisticsView : MenuSubView
	{
		private static SpriteFont titleFont;
		private static SpriteFont itemFont;
		private static SpriteFont resetFont;
		private static Texture2D[] suitTextures = new Texture2D[4];

		public static int ContentCount()
		{
			return 7;
		}

		public static void LoadContent(ContentManager content, ContentLoadNotificationDelegate callback)
		{
			titleFont = content.Load<SpriteFont>(@"Menu\MainMenuFont");
			callback();
			itemFont = content.Load<SpriteFont>(@"Menu\StatisticsFont");
			callback();
			resetFont = content.Load<SpriteFont>(@"Menu\MenuSubFont");
			callback();

			string[] suits = {"Spade", "Diamond", "Club", "Heart" };
			for (int i = 0; i < suits.Length; i++)
			{
				suitTextures[i] = content.Load<Texture2D>(@"ThemePacks\Original\Card\" + suits[i]);
				callback();
			}
		}

		private Rectangle viewRect;
		private List<MenuButton> labels = new List<MenuButton>();
		private TextMenuButton resetButton;

		public StatisticsView(Rectangle rc)
		{
			Statistics.Load();
			viewRect = rc;

			InitControls();
		}

		protected void InitControls()
		{
			labels.Clear();

			int x = 10;
			int y = 10;
			int xSpacing = (int) (viewRect.Width/20);
			int ySpacing = Math.Min(viewRect.Height / 20, 10);

			int xStartTable = (int)(viewRect.Width*0.7);
			int xTableSpacing = viewRect.Width/12;

			TextMenuButton titleLabel = new TextMenuButton() {Text = Strings.Statistics, Font = titleFont};
			Vector2 titleSize = titleLabel.Font.MeasureString(titleLabel.Text);
			titleLabel.Rect = new Rectangle(x, y, (int) titleSize.X, (int) titleSize.Y);
			labels.Add(titleLabel);

			y += Math.Max((int)titleSize.Y, xTableSpacing) + ySpacing - xTableSpacing;

			var easyHeaderIconSpade = new CustomMenuButton((btn, batch, rect) => { batch.Draw(suitTextures[0], rect, Color.White); });
			easyHeaderIconSpade.Rect = new Rectangle(xStartTable + xTableSpacing / 4, y + xTableSpacing / 4, xTableSpacing / 2, xTableSpacing / 2);
			labels.Add(easyHeaderIconSpade);

			var mediumHeaderIconSpade = new CustomMenuButton((btn, batch, rect) => { batch.Draw(suitTextures[0], rect, Color.White); });
			mediumHeaderIconSpade.Rect = new Rectangle(xStartTable + xTableSpacing, y + xTableSpacing / 4, xTableSpacing / 2, xTableSpacing / 2);
			labels.Add(mediumHeaderIconSpade);

			var mediumHeaderIconDiamond = new CustomMenuButton((btn, batch, rect) => { batch.Draw(suitTextures[1], rect, Color.White); });
			mediumHeaderIconDiamond.Rect = new Rectangle(xStartTable + xTableSpacing + xTableSpacing / 2, y + xTableSpacing / 4, xTableSpacing / 2, xTableSpacing / 2);
			labels.Add(mediumHeaderIconDiamond);

			var hardHeaderIconSpade = new CustomMenuButton((btn, batch, rect) => { batch.Draw(suitTextures[0], rect, Color.White); });
			hardHeaderIconSpade.Rect = new Rectangle(xStartTable + xTableSpacing * 2, y, xTableSpacing / 2, xTableSpacing / 2);
			labels.Add(hardHeaderIconSpade);

			var hardHeaderIconDiamond = new CustomMenuButton((btn, batch, rect) => { batch.Draw(suitTextures[1], rect, Color.White); });
			hardHeaderIconDiamond.Rect = new Rectangle(xStartTable + xTableSpacing * 2 + xTableSpacing / 2, y, xTableSpacing / 2, xTableSpacing / 2);
			labels.Add(hardHeaderIconDiamond);

			var hardHeaderIconClub = new CustomMenuButton((btn, batch, rect) => { batch.Draw(suitTextures[2], rect, Color.White); });
			hardHeaderIconClub.Rect = new Rectangle(xStartTable + xTableSpacing * 2, y + xTableSpacing / 2, xTableSpacing / 2, xTableSpacing / 2);
			labels.Add(hardHeaderIconClub);

			var hardHeaderIconHeart = new CustomMenuButton((btn, batch, rect) => { batch.Draw(suitTextures[3], rect, Color.White); });
			hardHeaderIconHeart.Rect = new Rectangle(xStartTable + xTableSpacing * 2 + xTableSpacing / 2, y + xTableSpacing / 2, xTableSpacing / 2, xTableSpacing / 2);
			labels.Add(hardHeaderIconHeart);

			y += xTableSpacing;

			TextMenuButton totalGamesLabel = new TextMenuButton() {Text = Strings.Stats_TotalGames, Font = itemFont};
			Vector2 totalGamesSize = totalGamesLabel.Font.MeasureString(totalGamesLabel.Text);
			totalGamesLabel.Rect = new Rectangle(x, y, (int) totalGamesSize.X, (int) totalGamesSize.Y);
			labels.Add(totalGamesLabel);

			{
				TextMenuButton info = new TextMenuButton { Text = Statistics.EasyGames.ToString(), Font = itemFont };
				info.CenterTextInRectangle(new Rectangle(xStartTable, y, xTableSpacing, 0));
				labels.Add(info);
			}
			{
				TextMenuButton info = new TextMenuButton { Text = Statistics.MediumGames.ToString(), Font = itemFont };
				info.CenterTextInRectangle(new Rectangle(xStartTable + xTableSpacing, y, xTableSpacing, 0));
				labels.Add(info);
			}
			{
				TextMenuButton info = new TextMenuButton { Text = Statistics.HardGames.ToString(), Font = itemFont };
				info.CenterTextInRectangle(new Rectangle(xStartTable + xTableSpacing * 2, y, xTableSpacing, 0));
				labels.Add(info);
			}

			y += (int)totalGamesSize.Y + ySpacing;

			TextMenuButton gamesWonLabel = new TextMenuButton() {Text = Strings.Stats_GamesWon, Font = itemFont};
			Vector2 gamesWonSize = gamesWonLabel.Font.MeasureString(totalGamesLabel.Text);
			gamesWonLabel.Rect = new Rectangle(x, y, (int)gamesWonSize.X, (int)gamesWonSize.Y);
			labels.Add(gamesWonLabel);

			{
				TextMenuButton info = new TextMenuButton { Text = Statistics.EasyGamesWon.ToString(), Font = itemFont };
				info.CenterTextInRectangle(new Rectangle(xStartTable, y, xTableSpacing, 0));
				labels.Add(info);
			}
			{
				TextMenuButton info = new TextMenuButton { Text = Statistics.MediumGamesWon.ToString(), Font = itemFont };
				info.CenterTextInRectangle(new Rectangle(xStartTable + xTableSpacing, y, xTableSpacing, 0));
				labels.Add(info);
			}
			{
				TextMenuButton info = new TextMenuButton { Text = Statistics.HardGamesWon.ToString(), Font = itemFont };
				info.CenterTextInRectangle(new Rectangle(xStartTable + xTableSpacing * 2, y, xTableSpacing, 0));
				labels.Add(info);
			}

			y += (int)gamesWonSize.Y + ySpacing;

			TextMenuButton winRateLabel = new TextMenuButton() {Text = Strings.Stats_WinRate, Font = itemFont};
			Vector2 winRateSize = winRateLabel.Font.MeasureString(totalGamesLabel.Text);
			winRateLabel.Rect = new Rectangle(x, y, (int)winRateSize.X, (int)winRateSize.Y);
			labels.Add(winRateLabel);

			{
				TextMenuButton info = new TextMenuButton { Text = WinRateString(Statistics.EasyGamesWon, Statistics.EasyGames), Font = itemFont };
				info.CenterTextInRectangle(new Rectangle(xStartTable, y, xTableSpacing, 0));
				labels.Add(info);
			}
			{
				TextMenuButton info = new TextMenuButton { Text = WinRateString(Statistics.MediumGamesWon, Statistics.MediumGames), Font = itemFont };
				info.CenterTextInRectangle(new Rectangle(xStartTable + xTableSpacing, y, xTableSpacing, 0));
				labels.Add(info);
			}
			{
				TextMenuButton info = new TextMenuButton { Text = WinRateString(Statistics.HardGamesWon, Statistics.HardGames), Font = itemFont };
				info.CenterTextInRectangle(new Rectangle(xStartTable + xTableSpacing * 2, y, xTableSpacing, 0));
				labels.Add(info);
			}

			y += (int)winRateSize.Y + ySpacing * 3;

			TextMenuButton totalTimeLabel = new TextMenuButton() {Text = Strings.Stats_TotalTimeLabel, Font = itemFont};
			Vector2 totalTimeSize = totalTimeLabel.Font.MeasureString(totalTimeLabel.Text);
			totalTimeLabel.Rect = new Rectangle(x, y, (int) totalTimeSize.X, (int) totalTimeSize.Y);
			labels.Add(totalTimeLabel);

			{
				TextMenuButton info = new TextMenuButton { Text = TimeSpan.FromSeconds(Statistics.TotalTimePlayed).ToString(), Font = itemFont };
				info.CenterTextInRectangle(new Rectangle(xStartTable, y, xTableSpacing * 3, 0));
				labels.Add(info);
			}

			resetButton = new TextMenuButton() {Text = Strings.Stats_ResetButton, Font = resetFont};
			Vector2 resetSize = resetButton.Font.MeasureString(resetButton.Text);
			resetButton.Rect = new Rectangle(viewRect.Width - (int)resetSize.X - xSpacing, viewRect.Height - (int)resetSize.Y - ySpacing, (int)resetSize.X, (int)resetSize.Y);
			resetButton.ButtonClickDelegate = OnResetClicked;
		}

		protected string WinRateString(int won, int total)
		{
			return string.Format("{0}%", (total == 0 ? 0 : (int) ((float) won/total*100)));
		}

		public void Update()
		{
			foreach (TouchLocation touchLoc in TouchPanel.GetState())
			{
				Point pt = new Point((int) touchLoc.Position.X, (int) touchLoc.Position.Y);

				if (touchLoc.State == TouchLocationState.Released)
				{
					if (resetButton.Rect.Contains(pt))
					{
						if (resetButton.ButtonClickDelegate != null)
							resetButton.ButtonClickDelegate(resetButton);
						break;
					}
				}
			}
		}

		public void Render(Rectangle rect, SpriteBatch batch)
		{
			batch.Begin();
			foreach (MenuButton label in labels)
			{
				if (label.Visible)
				{
					TextMenuButton textButton = label as TextMenuButton;
					if (textButton != null)
					{
						Vector2 pos = new Vector2(label.Rect.X, label.Rect.Y);
						batch.DrawString(textButton.Font, textButton.Text, pos, label.Color);
					}
					var customButton = label as CustomMenuButton;
					if (customButton != null)
					{
						customButton.Render(batch);
					}
				}
			}

			{
				Vector2 pos = new Vector2(resetButton.Rect.X, resetButton.Rect.Y);
				batch.DrawString(resetButton.Font, resetButton.Text, pos, resetButton.Color);
			}

			batch.End();
		}

		private void OnResetClicked(MenuButton button)
		{
			Statistics.Reset();
			Statistics.Save();
			InitControls();
			Analytics.RegisterEvent(Analytics.EventType.ResetStatistics);
		}

		public void OnClose()
		{
		}
	}

	internal class OptionsView : MenuSubView
	{
		private static SpriteFont titleFont;
		private static SpriteFont itemFont;
		
		private struct ThemeTexture
		{
			public const int Fields = 6;
			public Texture2D Front;
			public Texture2D Value;
			public Texture2D Suit;
			public Texture2D Back;
			public Texture2D HighlightEnd;
			public Texture2D HighlightCenter;
		}
		private static List<ThemeTexture> themeTextures;

		public static int ContentCount()
		{
			return 2 + themePacks.Length * ThemeTexture.Fields;
		}

		public static void LoadContent(ContentManager content, ContentLoadNotificationDelegate callback)
		{
			titleFont = content.Load<SpriteFont>(@"Menu\MainMenuFont");
			callback();
			itemFont = content.Load<SpriteFont>(@"Menu\StatisticsFont");
			callback();

			themeTextures = new List<ThemeTexture>();
			foreach (var themeInfo in themePacks)
			{
				var front = content.Load<Texture2D>(@"ThemePacks\" + themeInfo + @"\Card\Card");
				callback();
				var value = content.Load<Texture2D>(@"ThemePacks\" + themeInfo + @"\Card\A");
				callback();
				var suit = content.Load<Texture2D>(@"ThemePacks\" + themeInfo + @"\Card\Spade");
				callback();
				var back = content.Load<Texture2D>(@"ThemePacks\" + themeInfo + @"\Card\CardBack_White");
				callback();
				var center = content.Load<Texture2D>(@"ThemePacks\" + themeInfo + @"\Card\Highlight_Center");
				callback();
				var end = content.Load<Texture2D>(@"ThemePacks\" + themeInfo + @"\Card\Highlight_End");
				callback();
				themeTextures.Add(new ThemeTexture { Front = front, Value = value, Suit = suit, Back = back, HighlightCenter = center, HighlightEnd = end });
			}
		}

		private static Color[] deckColors = new Color[]
		{Color.CornflowerBlue, Color.Crimson, Color.LightSlateGray, Color.Gold, Color.MediumPurple, Color.Silver};

		private static string[] themePacks = { "Original", "Modern", "Dark" };

		private Rectangle viewRect;
		private List<MenuButton> labels = new List<MenuButton>();
		private List<CustomMenuButton> deckColorButtons = new List<CustomMenuButton>();
		private List<CustomMenuButton> themePackButtons = new List<CustomMenuButton>();

		private int selectedDeckColor = 0;
		private int selectedTheme = 0;

		private Texture2D currentCardBack;

		public OptionsView(Rectangle rc)
		{
			Options.Load();
			viewRect = rc;

			for (int i = 0; i < deckColors.Length; i++)
			{
				if (deckColors[i] == Options.CardBackColor)
				{
					selectedDeckColor = i;
					break;
				}
			}

			for (int i = 0; i < themePacks.Length; i++)
			{
				if (themePacks[i] == Options.ThemePack)
				{
					selectedTheme = i;
					break;
				}
			}

			currentCardBack = themeTextures[selectedTheme].Back;

			InitControls();
		}

		protected void InitControls()
		{
			labels.Clear();

			int x = 20;
			int y = 20;
			int xSpacing = (int) (viewRect.Width/20);
			int ySpacing = (int) (viewRect.Height/10);
			int xMaxLabel = 0;

			TextMenuButton titleLabel = new TextMenuButton() {Text = Strings.Options, Font = titleFont};
			Vector2 titleSize = titleLabel.Font.MeasureString(titleLabel.Text);
			titleLabel.Rect = new Rectangle(x, y, (int) titleSize.X, (int) titleSize.Y);
			if (titleLabel.Rect.Right > xMaxLabel)
				xMaxLabel = titleLabel.Rect.Right;
			labels.Add(titleLabel);

			int cardHeight = (int)(viewRect.Width / 8);
			int cardWidth = (int)(cardHeight * ((float)currentCardBack.Width / (float)currentCardBack.Height));
			
			y = (int)titleSize.Y;

			TextMenuButton themeLabel = new TextMenuButton() { Text = Strings.Options_ThemeLabel, Font = itemFont };
			Vector2 themeLabelSize = themeLabel.Font.MeasureString(themeLabel.Text);
			themeLabel.Rect = new Rectangle(x, y + ySpacing + (int)(cardHeight - themeLabelSize.Y) / 2, (int)themeLabelSize.X, (int)themeLabelSize.Y);
			if (themeLabel.Rect.Right > xMaxLabel)
				xMaxLabel = themeLabel.Rect.Right;
			labels.Add(themeLabel);

			for (int i = 0; i < themePacks.Length; i++)
			{
				var front = themeTextures[i].Front;
				var value = themeTextures[i].Value;
				var suit = themeTextures[i].Suit;
				var button = new CustomMenuButton((btn, batch, rect) => { RenderCard(batch, rect, front, value, suit); })
				{
					ButtonClickDelegate = OnThemePackClicked
				};
				button.Rect = new Rectangle(xMaxLabel - xSpacing + (cardWidth + xSpacing / 2) * i, y + ySpacing,
					cardWidth, cardHeight);
				themePackButtons.Add(button);
			}

			y = y + ySpacing + cardHeight;
			
			TextMenuButton deckColorLabel = new TextMenuButton() {Text = Strings.Options_DeckColorLabel, Font = itemFont};
			Vector2 deckColorSize = deckColorLabel.Font.MeasureString(deckColorLabel.Text);
			deckColorLabel.Rect = new Rectangle(x, y + ySpacing + (int)(cardHeight - deckColorSize.Y) / 2, (int) deckColorSize.X, (int) deckColorSize.Y);
			if (deckColorLabel.Rect.Right > xMaxLabel)
				xMaxLabel = deckColorLabel.Rect.Right;
			labels.Add(deckColorLabel);

			for (int i = 0; i < deckColors.Length; i++)
			{
				var button = new CustomMenuButton((btn, batch, rect) => batch.Draw(currentCardBack, rect, btn.Color))
				{
					ButtonClickDelegate = OnDeckColorClicked,
					Color = deckColors[i]
				};
				button.Rect = new Rectangle(xMaxLabel - xSpacing + (cardWidth + xSpacing/2)*i, y + ySpacing,
					cardWidth, cardHeight);
				deckColorButtons.Add(button);
			}
		}

		private void RenderCard(SpriteBatch batch, Rectangle rect, Texture2D frontTexture, Texture2D valueTexture, Texture2D suitTexture)
		{
			batch.Draw(frontTexture, rect, Color.White);

			int valueWidth = rect.Width / 6 + 3;
			int valueHeight = valueWidth * 55 / 45;
			Rectangle numberRect = new Rectangle(rect.X + 5, rect.Y + 5, valueWidth, valueHeight);
			batch.Draw(valueTexture, numberRect, Color.White);

			Rectangle suitSmallRect = new Rectangle(numberRect.X + numberRect.Width, numberRect.Y, numberRect.Height,
				numberRect.Height);
			batch.Draw(suitTexture, suitSmallRect, Color.White);

			Rectangle numberTexRect = valueTexture.Bounds;
			Rectangle numberBottomRect = new Rectangle(rect.Right - numberRect.Width - 5, rect.Bottom - numberRect.Height - 5,
				numberRect.Width, numberRect.Height);
			batch.Draw(valueTexture, numberBottomRect, null, Color.White, 0.0f, Vector2.Zero,
				SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, 0);

			Rectangle suitTexRect = suitTexture.Bounds;
			Rectangle suitBottomRect = new Rectangle(numberBottomRect.Left - suitSmallRect.Width,
				rect.Bottom - suitSmallRect.Height - 5, suitSmallRect.Width, suitSmallRect.Height);
			batch.Draw(suitTexture, suitBottomRect, null, Color.White, 0.0f, Vector2.Zero,
				SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, 0);

			Rectangle suitLargeRect = new Rectangle(rect.X + rect.Width / 6, rect.Y + rect.Height / 2 - (rect.Width / 3),
				2 * rect.Width / 3, 2 * rect.Width / 3);
			batch.Draw(suitTexture, suitLargeRect, Color.White);
		}

		public void Update()
		{
			foreach (TouchLocation touchLoc in TouchPanel.GetState())
			{
				Point pt = new Point((int) touchLoc.Position.X, (int) touchLoc.Position.Y);

				if (touchLoc.State == TouchLocationState.Released)
				{
					foreach (var button in deckColorButtons)
					{
						if (button.Rect.Contains(pt))
						{
							if (button.ButtonClickDelegate != null)
								button.ButtonClickDelegate(button);
							break;
						}
					}
					foreach (var button in themePackButtons)
					{
						if (button.Rect.Contains(pt))
						{
							if (button.ButtonClickDelegate != null)
								button.ButtonClickDelegate(button);
							break;
						}
					}
				}
			}
		}

		public void Render(Rectangle rect, SpriteBatch batch)
		{
			batch.Begin();
			foreach (MenuButton label in labels)
			{
				if (label.Visible)
				{
					TextMenuButton textButton = label as TextMenuButton;
					if (textButton != null)
					{
						Vector2 pos = new Vector2(label.Rect.X, label.Rect.Y);
						batch.DrawString(textButton.Font, textButton.Text, pos, label.Color);
					}
				}
			}

			foreach (var button in deckColorButtons)
			{
				button.Render(batch);
			}

			Texture2D highlightEndTex = themeTextures[selectedTheme].HighlightEnd;
			Texture2D highlightCenterTex = themeTextures[selectedTheme].HighlightCenter;

			{
				var selectedButton = deckColorButtons[selectedDeckColor];
				Rectangle overlayRect = new Rectangle(selectedButton.Rect.X, selectedButton.Rect.Y, selectedButton.Rect.Width,
					selectedButton.Rect.Height);
				overlayRect.Inflate(overlayRect.Width/12, overlayRect.Height/12);

				Rectangle topRect = new Rectangle(overlayRect.Left, overlayRect.Top, overlayRect.Width, overlayRect.Y/4);
				Rectangle bottomRect = new Rectangle(overlayRect.Left, overlayRect.Bottom - topRect.Height, overlayRect.Width,
					topRect.Height);
				Rectangle centerRect = new Rectangle(overlayRect.Left, overlayRect.Top + topRect.Height, overlayRect.Width,
					overlayRect.Height - topRect.Height*2);

				batch.Draw(highlightEndTex, topRect, Color.White);
				batch.Draw(highlightEndTex, bottomRect, null, Color.White, 0.0f, Vector2.Zero,
					SpriteEffects.FlipVertically, 0.0f);
				batch.Draw(highlightCenterTex, centerRect, Color.White);
			}

			foreach (var button in themePackButtons)
			{
				button.Render(batch);
			}

			{
				CustomMenuButton selectedButton = themePackButtons[selectedTheme];
				Rectangle overlayRect = new Rectangle(selectedButton.Rect.X, selectedButton.Rect.Y, selectedButton.Rect.Width,
					selectedButton.Rect.Height);
				overlayRect.Inflate(overlayRect.Width / 12, overlayRect.Height / 12);

				Rectangle topRect = new Rectangle(overlayRect.Left, overlayRect.Top, overlayRect.Width, overlayRect.Y / 4);
				Rectangle bottomRect = new Rectangle(overlayRect.Left, overlayRect.Bottom - topRect.Height, overlayRect.Width,
					topRect.Height);
				Rectangle centerRect = new Rectangle(overlayRect.Left, overlayRect.Top + topRect.Height, overlayRect.Width,
					overlayRect.Height - topRect.Height * 2);

				batch.Draw(highlightEndTex, topRect, Color.White);
				batch.Draw(highlightEndTex, bottomRect, null, Color.White, 0.0f, Vector2.Zero,
					SpriteEffects.FlipVertically, 0.0f);
				batch.Draw(highlightCenterTex, centerRect, Color.White);
			}

			batch.End();
		}

		private void OnDeckColorClicked(MenuButton button)
		{
			for (int i = 0; i < deckColorButtons.Count; i++)
			{
				if (button == deckColorButtons[i])
					selectedDeckColor = i;
			}
			Options.CardBackColor = deckColors[selectedDeckColor];
		}

		private void OnThemePackClicked(MenuButton button)
		{
			for (int i = 0; i < themePackButtons.Count; i++)
			{
				if (button == themePackButtons[i])
					selectedTheme = i;
			}
			Options.ThemePack = themePacks[selectedTheme];
			currentCardBack = themeTextures[selectedTheme].Back;
		}

		public void OnClose()
		{
			Options.Save();
			CardResources.LoadResources(GameStateManager.GraphicsDevice, GameStateManager.Content, () => { });
		}
	}

	internal class AboutView : MenuSubView
	{
		private static SpriteFont titleFont;
		private static SpriteFont itemFont;

		public static int ContentCount()
		{
			return 2;
		}

		public static void LoadContent(ContentManager content, ContentLoadNotificationDelegate callback)
		{
			titleFont = content.Load<SpriteFont>(@"Menu\MainMenuFont");
			callback();
			itemFont = content.Load<SpriteFont>(@"Menu\AboutFont");
			callback();
		}

		private Rectangle viewRect;
		private List<MenuButton> labels = new List<MenuButton>();
		private MessageWindow messageWindow;

		public AboutView(Rectangle rc)
		{
			viewRect = rc;

			InitControls();
		}

		protected void InitControls()
		{
			labels.Clear();

			int x = 40;
			int y = viewRect.Height/10;
			int xSpacing = (int) (viewRect.Width/20);
			int ySpacing = (int) (viewRect.Height*0.09);
			int xMaxLabel = 0;

			TextMenuButton titleLabel = new TextMenuButton() {Text = Strings.About_Title, Font = titleFont};
			Vector2 titleSize = titleLabel.Font.MeasureString(titleLabel.Text);
			titleLabel.Rect = new Rectangle(x, y, (int) titleSize.X, (int) titleSize.Y);
			labels.Add(titleLabel);

			TextMenuButton versionLabel = new TextMenuButton() {Text = Strings.About_VersionLabel, Font = itemFont};
			Vector2 versionSize = versionLabel.Font.MeasureString(versionLabel.Text);
			versionLabel.Rect = new Rectangle(x + xSpacing, y + ySpacing*2, (int) versionSize.X, versionLabel.Font.LineSpacing);
			if (versionLabel.Rect.Right > xMaxLabel)
				xMaxLabel = versionLabel.Rect.Right;
			labels.Add(versionLabel);

			TextMenuButton copyrightLabel = new TextMenuButton() {Text = Strings.About_CopyrightLabel, Font = itemFont};
			Vector2 copyrightSize = copyrightLabel.Font.MeasureString(copyrightLabel.Text);
			copyrightLabel.Rect = new Rectangle(x + xSpacing, y + ySpacing*3, (int) copyrightSize.X,
				copyrightLabel.Font.LineSpacing);
			if (copyrightLabel.Rect.Right > xMaxLabel)
				xMaxLabel = copyrightLabel.Rect.Right;
			labels.Add(copyrightLabel);

			TextMenuButton fontCopyrightLabel = new TextMenuButton() {Text = Strings.About_FontCopyrightLabel, Font = itemFont};
			Vector2 fontCopyrightSize = fontCopyrightLabel.Font.MeasureString(copyrightLabel.Text);
			fontCopyrightLabel.Rect = new Rectangle(x + xSpacing, y + ySpacing*4, (int) fontCopyrightSize.X,
				fontCopyrightLabel.Font.LineSpacing);
			if (fontCopyrightLabel.Rect.Right > xMaxLabel)
				xMaxLabel = fontCopyrightLabel.Rect.Right;
			labels.Add(fontCopyrightLabel);

			Dictionary<string, string> assemblyInfo = GetAssemblyInfo();
			string versionStr = assemblyInfo["Version"];
			TextMenuButton versionInfo = new TextMenuButton() {Text = versionStr, Font = itemFont};
			Vector2 versionInfoSize = versionInfo.Font.MeasureString(versionInfo.Text);
			versionInfo.Rect = new Rectangle(xMaxLabel + xSpacing, y + ySpacing*2, (int) versionInfoSize.X,
				versionInfo.Font.LineSpacing);
			labels.Add(versionInfo);

			TextMenuButton copyrightInfo = new TextMenuButton() {Text = Strings.About_CopyrightInfo, Font = itemFont};
			Vector2 copyrightInfoSize = copyrightInfo.Font.MeasureString(copyrightInfo.Text);
			copyrightInfo.Rect = new Rectangle(xMaxLabel + xSpacing, y + ySpacing*3, (int) copyrightInfoSize.X,
				copyrightInfo.Font.LineSpacing);
			labels.Add(copyrightInfo);

			TextMenuButton fontCopyrightInfo = new TextMenuButton() {Text = Strings.About_FontCopyrightInfo, Font = itemFont};
			Vector2 fontCopyrightInfoSize = fontCopyrightInfo.Font.MeasureString(fontCopyrightInfo.Text);
			fontCopyrightInfo.Rect = new Rectangle(xMaxLabel + xSpacing, y + ySpacing*4, (int) fontCopyrightInfoSize.X,
				fontCopyrightInfo.Font.LineSpacing);
			labels.Add(fontCopyrightInfo);

			if (GameStateManager.IsTrial)
			{
				TextMenuButton upgradeLabel = new TextMenuButton() {Text = MessageWindow.BreakStringIntoLines(Strings.About_UpgradeLabel, viewRect.Width, itemFont), Font = itemFont};
				Vector2 upgradeSize = upgradeLabel.Font.MeasureString(upgradeLabel.Text);
				upgradeLabel.Rect = new Rectangle((viewRect.Width - (int) upgradeSize.X)/2, viewRect.Height - (int)upgradeSize.Y, (int) upgradeSize.X,
					upgradeLabel.Font.LineSpacing);
				upgradeLabel.ButtonClickDelegate = OnUpgradeClicked;
				labels.Add(upgradeLabel);

				TextMenuButton trialLabel = new TextMenuButton() {Text = Strings.About_TrialModeLabel, Font = itemFont};
				Vector2 trialSize = trialLabel.Font.MeasureString(trialLabel.Text);
				trialLabel.Rect = new Rectangle((viewRect.Width - (int) trialSize.X)/2, upgradeLabel.Rect.Y - (int)trialSize.Y, (int) trialSize.X,
					trialLabel.Font.LineSpacing);
				labels.Add(trialLabel);
			}
		}

		protected Dictionary<string, string> GetAssemblyInfo()
		{
			Dictionary<string, string> assemblyInfo = new Dictionary<string, string>();

			string fullInfo = Assembly.GetExecutingAssembly().FullName;
			foreach (string v in fullInfo.Split(new[] {',', ' '}, StringSplitOptions.RemoveEmptyEntries))
			{
				string[] args = v.Split('=');
				assemblyInfo.Add(args[0], (args.Length > 1 ? args[1] : ""));
			}

			return assemblyInfo;
		}

		public void Update()
		{
			if (messageWindow != null)
			{
				if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || messageWindow.Update() == false)
				{
					messageWindow = null;
					return;
				}
			}
			else
			{
				foreach (TouchLocation touchLoc in TouchPanel.GetState())
				{
					Point pt = new Point((int) touchLoc.Position.X, (int) touchLoc.Position.Y);

					if (touchLoc.State == TouchLocationState.Released)
					{
						foreach (MenuButton button in labels)
						{
							if (button.Rect.Contains(pt))
							{
								if (button.ButtonClickDelegate != null)
									button.ButtonClickDelegate(button);
								break;
							}
						}
					}
				}
			}
		}

		public void Render(Rectangle rect, SpriteBatch batch)
		{
			batch.Begin();
			foreach (MenuButton label in labels)
			{
				if (label.Visible)
				{
					TextMenuButton textButton = label as TextMenuButton;
					if (textButton != null)
					{
						Vector2 pos = new Vector2(label.Rect.X, label.Rect.Y);
						batch.DrawString(textButton.Font, textButton.Text, pos, label.Color);
					}
				}
			}

			batch.End();

			if (messageWindow != null)
			{
				messageWindow.Render(rect, batch);
			}
		}

		public void OnClose()
		{
		}

		private void OnUpgradeClicked(MenuButton button)
		{
			TrialMode.LaunchMarketplace();
		}
	}

	internal class MessageWindow
	{
		private static Texture2D backgroundTex;
		private static SpriteFont font;

		public static int ContentCount()
		{
			return 2;
		}

		public static void LoadContent(ContentManager content, ContentLoadNotificationDelegate callback)
		{
			backgroundTex = content.Load<Texture2D>(@"Menu\MessageWindow");
			callback();
			font = content.Load<SpriteFont>(@"Menu\MessageFont");
			callback();
		}

		private Rectangle viewRect;
		private Rectangle windowRect;
		private Vector2 textPos;
		private string windowText;
		public OnClick ClickDelegate { get; set; }

		public MessageWindow(Rectangle viewRect, string text)
		{
			this.viewRect = viewRect;
			SetText(text);
		}

		protected void SetText(string text)
		{
			windowText = BreakStringIntoLines(text, (int) (viewRect.Width*0.75), font);

			Vector2 textSize = font.MeasureString(windowText);
			textPos = new Vector2((viewRect.Width - textSize.X)/2, (viewRect.Height - textSize.Y)/2);

			int xPadding = (int) (textSize.X*0.15);
			int yPadding = (int) (textSize.Y*0.15);
			windowRect = new Rectangle(
				(int) textPos.X - xPadding,
				(int) textPos.Y - yPadding,
				(int) textSize.X + xPadding*2,
				(int) textSize.Y + yPadding*2);
		}

		public static string BreakStringIntoLines(string text, int maxWidth, SpriteFont font)
		{
			var textLines = new List<string>();
			Vector2 size = font.MeasureString(text);
			int lines = (int)(size.X / maxWidth) + 1;
			while (lines > 1)
			{
				int index = text.LastIndexOf(' ', text.Length / lines);
				textLines.Add(text.Substring(0, index));
				text = text.Substring(index);
				lines--;
			}
			textLines.Add(text);
			return string.Join("\n", textLines);
		}

		public virtual bool Update()
		{
			foreach (TouchLocation touchLoc in TouchPanel.GetState())
			{
				Point pt = new Point((int) touchLoc.Position.X, (int) touchLoc.Position.Y);
				if (touchLoc.State == TouchLocationState.Released)
				{
					if (windowRect.Contains(pt))
					{
						ClickDelegate();
						return false;
					}
				}
			}
			return true;
		}

		public void Render(Rectangle rect, SpriteBatch batch)
		{
			Color overlayColor = Color.Multiply(Color.Black, 0.8f);

			batch.Begin();

			batch.Draw(CardResources.BlankTex, viewRect, overlayColor);
			batch.Draw(backgroundTex, windowRect, Color.White);
			batch.DrawString(font, string.Join("\n", windowText), textPos, Color.White);

			batch.End();
		}
	}
}