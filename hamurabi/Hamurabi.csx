//
// Hamurabi by Bill Maya
// Converted from 8k Microsoft Basic version of game
//

using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;

Random rand = new Random(DateTime.Now.Minute);

const bool DEBUG_RATS = false;
const bool DEBUG_PLAGUE = false;
const bool DEBUG_STARVED = false;

const int maxYears = 10;
const int bushelsPerPerson = 20; // Number of bushels needed to feed one person
const int acresPlantedByPerson = 10; // Number of acres a single person can plant
const int acresSeededPerBushel = 2; // Number of acres that can be seeded by a single bushel
const double chanceOfPlague = .15; // Change of plague occuring (.15 = 15%)
const double starvationLowerLimit = .45; // Lower boundery for mass starvation (.45 = 45%)

enum PlayerActions { Buy, Sell, Feed, Plant }
PlayerActions nextPlayerAction;

int currentYear;
int population;
int immigrants;
int fed;
int starved;
int deaths;
double percentStarved;

double plagueNumerator;
double plagueDiceRoll;
bool plagueOutbreak;
bool massStarvation;

int harvest;
int bushels;
int bushelsPerAcre;
int eatenByRats;
int acres;
int startingAcres;
int startingAcresPerPerson;
int endingAcresPerPerson;

bool acresBought;
int acresToBuy;
int acresToSell;
int peopleToFeed;
int acresToPlant;

// 
// Title area
//
Label title = new Label() { Text = "Hamurabi", Margin = new Thickness(0, 18, 0, 0), HorizontalOptions = LayoutOptions.Center };

Label instructions = new Label {
	Text = "Try your hand at governing ancient Sumeria successfully for a " + maxYears + " year term of office." 
};

var beginButton = new Button { 
	Text = "Begin", 
	BorderWidth = 1, 
	WidthRequest = 75,
	HorizontalOptions = LayoutOptions.Center
};

beginButton.Clicked += (s, e) => { StartGame(); };

//
// Display area
//
Label yearLabel = new Label() { Text = "Year:" };
Label peopleWhoStarvedLabel = new Label() { Text = "People who starved: " };
Label immigrantsLabel = new Label() { Text = "Immigrants: " };
Label populationLabel = new Label() { Text = "Population: " };
Label acresOwnedLabel = new Label() { Text = "Acres owned by city: " };
Label bushelsHarvestedLabel = new Label() { Text = "Bushels harvested per acre: " };
Label bushelsEatenLabel = new Label() { Text = "Bushels eaten by rats: " };
Label bushelsStoredLabel = new Label() { Text = "Bushels in storage: " };
Label acreCostInBushelsLabel = new Label() { Text = "Acre cost in bushels: " };
Label tenYearReport = new Label { Text = "" }; // End game report

//
// Control area
//
Label acresToBuyLabel = new Label() { Text = "Acres to buy: " };
Label acresToSellLabel = new Label() { Text = "Acres to sell: " };
Label peopleToFeedLabel = new Label() { Text = "People to feed: " };
Label acresToPlantLabel = new Label() { Text = "Acres to plant: " };

Slider acresToBuySlider = new Slider() { WidthRequest = 600, IsEnabled = false };
Slider acresToSellSlider = new Slider() { WidthRequest = 600, IsEnabled = false };
Slider peopleToFeedSlider = new Slider() { WidthRequest = 600, IsEnabled = false };
Slider acresToPlantSlider = new Slider() { WidthRequest = 600, IsEnabled = false };

acresToBuySlider.ValueChanged += (s, e) => { 
	acresToBuyLabel.Text = UpdateLabelWithAmount(acresToBuyLabel, (int)acresToBuySlider.Value); 
};

acresToSellSlider.ValueChanged += (s, e) => {
	acresToSellLabel.Text = UpdateLabelWithAmount(acresToSellLabel, (int)acresToSellSlider.Value);
};

peopleToFeedSlider.ValueChanged += (s, e) => {
	peopleToFeedLabel.Text = UpdateLabelWithAmount(peopleToFeedLabel,(int)peopleToFeedSlider.Value);
};

acresToPlantSlider.ValueChanged += (s, e) => {
	acresToPlantLabel.Text = UpdateLabelWithAmount(acresToPlantLabel, (int)acresToPlantSlider.Value);
};

var buyButton = new Button {
	Text = "Buy",
	BorderWidth = 1,
	WidthRequest = 75,
	IsEnabled = false
};

var sellButton = new Button {
	Text = "Sell",
	BorderWidth = 1,
	WidthRequest = 75,
	IsEnabled = false 
};

var feedButton = new Button { 
	Text = "Feed",
	BorderWidth = 1,
	WidthRequest = 75,
	IsEnabled = false 
};

var plantButton = new Button {
	Text = "Plant",
	BorderWidth = 1,
	WidthRequest = 75,
	IsEnabled = false 
};

buyButton.Clicked += (s, e) => { BuyAcres(); };
sellButton.Clicked += (s, e) => { SellAcres(); };
feedButton.Clicked += (s, e) => { FeedPeople(); };
plantButton.Clicked += (s, e) => { PlantAcres(); };

//
// Screen setup
//
var screenStack = new StackLayout {
	Orientation = StackOrientation.Vertical,
	Margin = 50,
	Children = {
		title,
		instructions,
		beginButton,
		new StackLayout { // displayStack
	 		Orientation = StackOrientation.Vertical,
			Children = {
	 			yearLabel,
	 			peopleWhoStarvedLabel,
	 			immigrantsLabel,
	 			populationLabel,
	 			acresOwnedLabel,
	 			bushelsHarvestedLabel,
	 			bushelsEatenLabel,
	 			bushelsStoredLabel,
				acreCostInBushelsLabel
			}
		},
		new StackLayout { // controlStack
			Orientation = StackOrientation.Vertical,
			Children = {
				new StackLayout {
					Orientation = StackOrientation.Horizontal,
					Children = {
						acresToBuySlider,
						buyButton,
						acresToBuyLabel
					}
				},
				new StackLayout {
					Orientation = StackOrientation.Horizontal,
					Children = {
						acresToSellSlider,
						sellButton,
						acresToSellLabel
					}
				},
				new StackLayout {
					Orientation = StackOrientation.Horizontal,
					Children = {
						peopleToFeedSlider,
						feedButton,
						peopleToFeedLabel
					}
				},
				new StackLayout {
					Orientation = StackOrientation.Horizontal,
					Children = {
						acresToPlantSlider,
						plantButton,
						acresToPlantLabel
					}
				} 
			}	
		}, // controlStack Children
		new StackLayout { // reportStack
			Children = {
				tenYearReport		
			}
		}
	} // screenStack Children
};

var Main = new ContentPage() {
		Content = screenStack
};

//
// Game methods
// TODO Refactor to add "better" game loop
//
void StartGame() {
	SetupData();
	
	beginButton.Text = "Reset";
	tenYearReport.Text = String.Empty;
	
	currentYear += 1;
	population += immigrants;
	bushelsPerAcre = rand.Next(0, 10) + 17;
	
	nextPlayerAction = PlayerActions.Buy;
	UpdateDisplay();
	UpdateControls();

}

void EndGame() {
	beginButton.Text = "Begin";
	tenYearReport.Text = CreateReport("GAME OVER");

	EnableBuyControls(false);
	EnableSellControls(false);
	EnableFeedControls(false);
	EnablePlantControls(false);
}

string CreateReport(string genericText) {
	string newLabelText = String.Empty;
	
	string starvation = String.Format("You starved {0} people in one year!", starved);
	
	string impeachment = String.Format("Due to this extreme mismanagement you have not only been impeached and thrown out of office but you have also been declared a National Fink!");
	
	string percentageStarved = String.Format("In your {0} year term of office, {1} percent of the popuation starved per year on average, i.e., a total of {2} people died!", maxYears, percentStarved, deaths);
	
	string acreage = String.Format("You started with {0} acres per person and ended with {1} acres per person.", startingAcresPerPerson, endingAcresPerPerson);
	
	string justRight = "A fantastic performance! Charlemange, Disraeli, and Jefferson combined could not have done better!";
	
	string couldHaveBeenBetter = String.Format("Your performance could have been somewhat better, but really wasn't too bad at all. {0} people would dearly like to see you assassinated but we all have our trivial problems.", (int)((double)population * 0.8 * rand.Next(1)));
	
	string heavyHanded = "Your heavy-handed performance smacks of Nero and Ivan IV. The people remaining find you an unpleasant ruler, and, frankly, hate your guts!";
	
	if (massStarvation) {
		newLabelText = String.Format("{0} {1}", starvation, impeachment);
	} else {
		newLabelText = String.Format("{0} {1}\n\n", percentageStarved, acreage);
		
		if (percentStarved > 33 || endingAcresPerPerson < 7) {
			newLabelText = String.Format("{0} {1}", newLabelText, impeachment);
		} else if (percentStarved > 10 || endingAcresPerPerson < 9) {
			newLabelText = String.Format("{0} {1}", newLabelText, heavyHanded);
		} else if (percentStarved > 3 || endingAcresPerPerson < 10) {
			newLabelText = String.Format("{0} {1}", newLabelText, couldHaveBeenBetter);
		} else {
			newLabelText = String.Format("{0} {1}", newLabelText, justRight);
		}
	}
	
	return newLabelText;
}

//
// Button methods
//
void BuyAcres() {
	acresToBuy = (int)acresToBuySlider.Value;
	acresBought = acresToBuy > 0 ? true : false;
	
	acres += acresToBuy;
	bushels -= (acresToBuy * bushelsPerAcre);

	// If you buy acres you can't immediately sell them the same turn
	if (acresBought) { 
		nextPlayerAction = PlayerActions.Feed;
	} else {
		nextPlayerAction = PlayerActions.Sell;	
	}
	
	UpdateDisplay();
	UpdateControls();

}

void SellAcres() {
	acresToSell = (int)acresToSellSlider.Value;
	acres -= acresToSell;
	bushels += (bushelsPerAcre * acresToSell);
	
	nextPlayerAction = PlayerActions.Feed;
	UpdateDisplay();
	UpdateControls();
}

void FeedPeople() {
	peopleToFeed = (int)peopleToFeedSlider.Value;
	bushels -= (peopleToFeed * bushelsPerPerson);
	
	nextPlayerAction = PlayerActions.Plant;
	UpdateDisplay();
	UpdateControls();
}

void PlantAcres() {
	acresToPlant = (int)acresToPlantSlider.Value;
	bushels -= (acresToPlant / 2);
	harvest = acresToPlant * bushelsPerAcre;
	
	int numerator = rand.Next(1,  6);
	if (((int)numerator / 2) != numerator / 2) {
		eatenByRats = 0;	// Rats don't eat grain 50% of time
	} else {
		int randomRatFactor = rand.Next(1, 6);
		eatenByRats = (int)(bushels / randomRatFactor); // Rats running wild!
	}
	bushels += (harvest - eatenByRats);
	bushelsPerAcre = rand.Next(0, 10) + 17;
	
	if (DEBUG_RATS) { ConsoleDebug("rats"); }
	
	immigrants = ((rand.Next(1, 6) * (20 * acres + bushels)) / population) / 100 + 1;
	fed = peopleToFeed;
	
	starved = population - fed;
	
	// Will you be thrown out of office for starving too many people?
	if (starved > .45 * population) {
		massStarvation = true;
		EndGame();	
	} else {
		massStarvation = false;
	}
	
	percentStarved = ((currentYear - 1) * percentStarved + starved *  100 / population) / currentYear;
		
	if (DEBUG_STARVED) { ConsoleDebug("starved"); }
	
	population = fed;
	deaths += starved;
	population += immigrants;
	
	endingAcresPerPerson = acres / population;
	
	// Plague - generates random number between 1-20 and divides by 20 for percentage
	double plagueNumerator = rand.Next(1, 20);
	double plagueDiceRoll = plagueNumerator / 20;
			
	if (DEBUG_PLAGUE) { ConsoleDebug("plague"); }
	
	if (plagueDiceRoll <= chanceOfPlague) {
		plagueOutbreak = true;
		population = population / 2;
		//Console.WriteLine("PLAGUE!");
	} else {
		plagueOutbreak = false;
	}
	
	acresToBuy = 0;
	acresToSell = 0;
	peopleToFeed = 0;
	acresToPlant = 0;
	
	currentYear += 1;
	
	if (currentYear > maxYears) {
		EndGame();
	} else {
		nextPlayerAction = PlayerActions.Buy;
		UpdateDisplay();
		UpdateControls();	
	}
}

//
// Utility methods
//
void UpdateDisplay() {
	yearLabel.Text = UpdateLabelWithAmount(yearLabel, currentYear);
	peopleWhoStarvedLabel.Text = UpdateLabelWithAmount(peopleWhoStarvedLabel, starved);
	immigrantsLabel.Text = UpdateLabelWithAmount(immigrantsLabel, immigrants);
	populationLabel.Text = UpdateLabelWithAmount(populationLabel, population, plagueOutbreak);
	acresOwnedLabel.Text = UpdateLabelWithAmount(acresOwnedLabel, acres);
	bushelsHarvestedLabel.Text = UpdateLabelWithAmount(bushelsHarvestedLabel, bushelsPerAcre);
	bushelsEatenLabel.Text = UpdateLabelWithAmount(bushelsEatenLabel, eatenByRats);
	bushelsStoredLabel.Text = UpdateLabelWithAmount(bushelsStoredLabel, bushels);
	acreCostInBushelsLabel.Text = UpdateLabelWithAmount(acreCostInBushelsLabel, bushelsPerAcre);
}

string UpdateLabelWithAmount(Label label, int amount) {
	string originalLabelText = label.Text.Substring(0, label.Text.IndexOf(":") + 1);
	string newLabelText = originalLabelText + " " + amount.ToString();
	
	return newLabelText;
}

string UpdateLabelWithAmount(Label label, int amount1, double amount2) {
	string originalLabelText = label.Text.Substring(0, label.Text.IndexOf(":") + 1);
	string newLabelText = String.Format("{0} {1} {2}", originalLabelText, amount1.ToString(), amount2.ToString());
	
	return newLabelText;
}

string UpdateLabelWithAmount(Label label, int amount, bool plague) {
	string originalLabelText = label.Text.Substring(0, label.Text.IndexOf(":") + 1);
	string newLabelText = originalLabelText + " " + amount.ToString();
	
	if (plague) {
		newLabelText = newLabelText + " Plague!";
	}
	
	return newLabelText;
	
}

void UpdateControls() { // TODO Why is error thrown when you pass enum in as argument?
	switch (nextPlayerAction) {
		case PlayerActions.Buy:
			EnableBuyControls(true);
			EnableSellControls(false);
			EnableFeedControls(false);
			EnablePlantControls(false);
										
			acresToBuySlider.Minimum = 0;
			acresToBuySlider.Maximum = (int)(bushels / bushelsPerAcre);
			acresToBuySlider.Value = acresToBuySlider.Minimum;			acresToBuyLabel.Text = UpdateLabelWithAmount(acresToBuyLabel, acresToBuy);
			
			break;
		case PlayerActions.Sell:
			EnableBuyControls(false);
			EnableSellControls(true);
			EnableFeedControls(false);
			EnablePlantControls(false);
			
			acresToSellSlider.Minimum = 0;
			acresToSellSlider.Maximum = acres;
			acresToSellSlider.Value = acresToSellSlider.Minimum;			acresToSellLabel.Text = UpdateLabelWithAmount(acresToSellLabel, acresToSell);
							  
			break;
		case PlayerActions.Feed:
			EnableBuyControls(false);
			EnableSellControls(false);
			EnableFeedControls(true);
			EnablePlantControls(false);
			
			peopleToFeedSlider.Minimum = 0;
			
			int maxPopulationByBushel = bushels / bushelsPerPerson;
			
			peopleToFeedSlider.Maximum = Math.Min(population, maxPopulationByBushel);
			peopleToFeedSlider.Value = peopleToFeedSlider.Maximum;
			peopleToFeedLabel.Text = UpdateLabelWithAmount(peopleToFeedLabel, (int)peopleToFeedSlider.Value);
			
			break;
		case PlayerActions.Plant:
			EnableBuyControls(false);
			EnableSellControls(false);
			EnableFeedControls(false);
			EnablePlantControls(true);
			
			acresToPlantSlider.Minimum = 0;
			
			int maxAcresByBushel = 0;
			int maxAcresByPopulation = 0;
			
			int bushelsNeeded = acres / acresSeededPerBushel;
			
			if (bushelsNeeded <= bushels) {
				maxAcresByBushel = acres;
			} else {
				maxAcresByBushel = bushels * acresSeededPerBushel;
			}
			
			int populationNeeded = acres / acresPlantedByPerson;
			
			if (populationNeeded <= population) {
				maxAcresByPopulation = acres * acresPlantedByPerson;
			} else {
				maxAcresByPopulation = population * acresPlantedByPerson;
			}
			
			acresToPlantSlider.Maximum = Math.Min(maxAcresByBushel, maxAcresByPopulation);
			acresToPlantSlider.Value = acresToPlantSlider.Maximum;
			acresToPlantLabel.Text = UpdateLabelWithAmount(acresToPlantLabel, (int)acresToPlantSlider.Value);
			break;
	}
}

void EnableBuyControls(bool enabled) {
	acresToBuySlider.IsEnabled = enabled;
	acresToBuyLabel.IsEnabled = enabled;
	buyButton.IsEnabled = enabled;
}

void EnableSellControls(bool enabled) {
	acresToSellSlider.IsEnabled = enabled;
	acresToSellLabel.IsEnabled = enabled;
	sellButton.IsEnabled = enabled;
}

void EnableFeedControls(bool enabled) {
	peopleToFeedLabel.IsEnabled = enabled;
	peopleToFeedSlider.IsEnabled = enabled;
	feedButton.IsEnabled = enabled;
}

void EnablePlantControls(bool enabled) {
	acresToPlantLabel.IsEnabled = enabled;
	acresToPlantSlider.IsEnabled = enabled;
	plantButton.IsEnabled = enabled;
}

void ConsoleDebug(string debug) {

	switch (debug) {	
		case "rats":
			Console.WriteLine("Year: {0}", currentYear);
			Console.WriteLine("Acres To Plant: {0}", acresToPlant);
			Console.WriteLine("Bushels Per Acre: {0}", bushelsPerAcre);
			Console.WriteLine("Harvest: {0}", harvest);
			Console.WriteLine("Eaten By Rats: {0}", eatenByRats);
			Console.WriteLine("Bushels: {0}", bushels);			Console.WriteLine("\n");
			break;
		case "plague":
			Console.WriteLine("Year: {0}", currentYear);
			Console.WriteLine("Plague #: {0}", plagueNumerator);
			Console.WriteLine("Plague %: {0}", plagueDiceRoll);
			Console.WriteLine("Chance of Plague: {0}", chanceOfPlague);
					
			if (plagueDiceRoll <= chanceOfPlague) { Console.WriteLine("PLAGUE!"); }
	
			Console.WriteLine("\n");
			break;
		case "starved":
			Console.WriteLine("Year: {0}", currentYear);
			Console.WriteLine("Prior Total % Starved: {0}%", Math.Round(percentStarved, 1));
			Console.WriteLine("Starved This Year: {0}", starved);
			Console.WriteLine("Population: {0}", population);
		
			double yearlyPercentStarved = ((double)starved / (double)population) * 100;
			Console.WriteLine("Yearly % Starved: {0}", yearlyPercentStarved);
	
			Console.WriteLine("New Total % Starved: {0}%", Math.Round(percentStarved, 1));
			Console.WriteLine("\n");
			break;
		default:
			break;
	}	
}

//
// Game data initialization
//
void SetupData() {
	currentYear = 0;
	population = 95;
	immigrants = 5;
	
	fed = 0;
	starved = 0; // D in original source
	deaths = 0;
	percentStarved = 0;

	harvest = 3000;
	bushels = 2800;
	bushelsPerAcre = 3;
	eatenByRats = harvest - bushels;
	acres = harvest / bushelsPerAcre;
	startingAcres = acres;
	startingAcresPerPerson = startingAcres / (population + immigrants);
	endingAcresPerPerson = startingAcresPerPerson;

	acresToBuy = 0;
	acresToSell = 0;
	peopleToFeed = 0;
	acresToPlant = 0;
}





