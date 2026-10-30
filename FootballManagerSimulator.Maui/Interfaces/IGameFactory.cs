namespace FootballManagerSimulator.Maui.Interfaces;

public interface IGameFactory
{
	void FinaliseGameState();
	void IntitialiseGameState();
	void AddClubsMissingFromSave();
}