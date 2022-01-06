using System;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using Position = UnityEngine.Vector2Int;
using Minimax;
// BoardModel has the responsibility of keeping track of the current state of the play, know the rules of the game and
// send updates to all listeners when the state changes.
[Serializable]
public class BoardModel : IBoardModel
{
    private static BoardModel instance;

    private List<IBoardListener> listeners;
    private Dictionary<Piece, List<Position>> startPiecePosition; //Använder mig av "Dictionary" för att placear pjäserna vid start.
    private List<Player> players;

    private Position[] directions; //Alla möjliga vägar som finns runt en pjäs.
    private int nrOfPlayers;
    private int nrOfPieces;
    private int difficulty;
    public int playerIndex;
    private const int xMin = -8, yMin = -8, xMax = 8, yMax = 8;

    private Piece[,] gameboard; //Skapar en två dimensionellaray för spelbrädet 
    private BoardModel() // Skriver här i konstruktorn för att göra det lättare att läsa / mindre stökigt där uppe
    {
        listeners = new List<IBoardListener>();
        players = new List<Player>();
        startPiecePosition = new Dictionary<Piece, List<Position>>(); 

        directions = new Position[6];
        directions[0] = new Position(1,  0); //x  
        directions[1] = new Position(-1, 0); //-x 
        directions[2] = new Position(-1, 1); //-x,-y
        directions[3] = new Position(1, -1); //+x,-y
        directions[4] = new Position(0,  1); //y
        directions[5] = new Position(0, -1); //-y

        gameboard = Utility.MakeMatrix<Piece>(xMin, yMin, xMax, yMax);
    }

    public static BoardModel Instance()
    {
        if (instance == null)
            instance = new BoardModel();
        return instance;
    }
  public void StartGame(int numPlayers)
    {
        nrOfPlayers = numPlayers;
        nrOfPieces = PieceInfo.numPieces[nrOfPlayers]; // här tilldelar jag variabeln nrOfPices med det antalet pjäser det ska finnas per spelare. 
        InitPiecePos();
        InitPlayers();
        InitEmptyPos();
        InitInvalidPos();
        SetDifficulty(1);
        InitStartPositions();
       
    }

  public void AddListener(IBoardListener listener)
  {
        listeners.Add(listener); //Lägger in alla lyssnare i en lista
  }

  public void SetDifficulty(int difficulty)
  {
        this.difficulty = difficulty;
  }

  public bool SetPiece(Position pos, Piece piece) 
  {
        return true;
  }

  public Piece GetPiece(Position pos)
  {
        if ((pos.x <= xMin && pos.x >= xMax) && (pos.y <= yMin && pos.y >= yMax)) // Kollar ifall man har klickat utanför brädan
            return Piece.Invalid;
        return gameboard[pos.x, pos.y]; // Om det ovan inte stämmer returenrar vi en pjäs
  }

  public bool MovePiece(Position startPos, Position endPos)
  {
        if (LegalMove(startPos, endPos)) // Om LegalMove är sant ber vi alla lyssnare att updatera vart pjäsen står.
        {
            foreach (var listener in listeners)
            {
                listener.MovePiece(startPos, endPos);
            }
            gameboard[endPos.x, endPos.y] = gameboard[startPos.x, startPos.y];
            gameboard[startPos.x, startPos.y] = Piece.Empty; // Gamla positionen blir "empty"
            WinCheck(gameboard[endPos.x,endPos.y]);
            return true;
        }
        return false;
   }

  private bool LegalMove(Position startPos, Position endPos)
    {
        //Kallar på metoderna "FindAllJumpPositions och FindAllRegularPositions och sedan lägger in alla möjliga positioner i varsin lista för att sedan kombinera dem i samma lista
        List<Position> allJumpMoves = FindAllJumpPositions(startPos, new List<Position>());  
        List<Position> allRegularMoves = FindAllRegularPositions(startPos, new List<Position>());
        List<Position> selectedPieceLegalMove = new List<Position>();
        selectedPieceLegalMove.AddRange(allJumpMoves);
        selectedPieceLegalMove.AddRange(allRegularMoves);

        foreach (var position in selectedPieceLegalMove) //Går igenom alla möjliga drag för selekterade pjäs från listan
        {
            if (position == endPos) //Ifall slut positionen är lika med en av giltiga dragen returnerar vi sant och rensar alla listor
            {
                return true;
            }
        }

        return false;
    }

  public void MakeMove(Player player)
   {
        State newState = new State((Piece[,])gameboard.Clone()); //Klonar nuvarande brädet för att få den nuvarande tillståndet och sedan uppdatera det nedan. 
        IState AI = MiniMax.Select(newState, player, player, players, 0, difficulty, true); // Kör algoritmen

        Piece[,] AIBoard = AI.GetGameBoard(); //Hämta AI:s beräknade bräda
        Position AIStartPos = new Position(Int32.MaxValue, Int32.MaxValue);
        Position AIEndPos = new Position(Int32.MaxValue, Int32.MaxValue);

        for (int x = xMin; x <= xMax; x++)
        {
            for (int y = yMin; y <= yMax; y++)
            {
                if (gameboard[x, y] != AIBoard[x, y]) //Om den nuvarande brädet inte är lika med AI bräde (alltså beräknade). Körs sekvensen nedan. 
                {
                    if (AIBoard[x, y] == Piece.Empty)
                    {
                        AIStartPos.Set(x, y);
                    }
                    else
                    {
                        AIEndPos.Set(x, y);
                    }
                }
            }
        }

        gameboard = AIBoard; //uppdatera brädet

        foreach (var listener in listeners) //här görs uppdateringen i vyn
        {
            listener.MovePiece(AIStartPos, AIEndPos);
        }

        WinCheck(gameboard[AIEndPos.x, AIEndPos.y]);




  }
    public void SaveGame() //Sparar alla nödvändiga värden som behövs för att spara spelet
    {
        BinaryFormatter formatter = new BinaryFormatter();
        Stream stream = new FileStream(Application.persistentDataPath + "/ChineseCheckers.data", FileMode.Create); //Skapar en ny fil 
        //Lägger in dessa värden i filen
        formatter.Serialize(stream, gameboard); 
        formatter.Serialize(stream, nrOfPlayers);
        formatter.Serialize(stream, nrOfPieces);
        formatter.Serialize(stream, difficulty);
        stream.Close();
    } 

  public void LoadGame() //Laddar in alla sparade värden
  {
        string path = Application.persistentDataPath + "/ChineseCheckers.data";

        if (File.Exists(path)) // Går först igenom ifall filen existerar
        {
            InitEmptyPos();
            BinaryFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(path, FileMode.Open);
            gameboard = (Piece[,])formatter.Deserialize(stream);
            nrOfPlayers = (int)formatter.Deserialize(stream);
            nrOfPieces = (int)formatter.Deserialize(stream);
            difficulty = (int)formatter.Deserialize(stream);

            foreach (IBoardListener listener in listeners) // Placerar alla pjäser på samma positioner som dem sparades som. 
            {
                listener.NewGame(players);
                for (int x = xMin; x <= xMax; x++)
                {
                    for (int y = yMin; y <= yMax; y++)
                    {
                        if (gameboard[x, y] != Piece.Empty && gameboard[x, y] != Piece.Invalid)
                        {
                            Position p = new Position(x, y);
                            listener.PlacePiece(p, gameboard[x, y]);
                        }
                    }
                }
            }
            InitPlayers();
            InitInvalidPos();
            InitPiecePos();
            stream.Close();
        }
  }

private void InitEmptyPos() // Ger alla positioner ett värde av "empty"
{
    for (int x = xMin; x <= xMax; x++)
    {
        for (int y = yMin; y <= yMax; y++)
        {
            gameboard[x, y] = Piece.Empty;
        }
    }
}

private void InitStartPositions()
{
    foreach (IBoardListener listener in listeners)
    {
        listener.NewGame(players);
        for (int i = 0; i < nrOfPlayers; i++) // Denna for loopen körs så många gånger som det finns spelare, t.ex om det är 2 spelare körs om den 2 gånger
        {
            for (int j = 0; j < nrOfPieces; j++) // Denna for loopen körs så många gånger som det finns antal pjäser, t.ex om det är 15 pjäser körs om den 15 gånger
            {
                Position pos = startPiecePosition[PieceInfo.setups[nrOfPlayers][i]][j];
                listener.PlacePiece(startPiecePosition[PieceInfo.setups[nrOfPlayers][i]][j], PieceInfo.setups[nrOfPlayers][i]);  // placera ut alla pjäser på brädan genom att först hämta ut nyckeln till ordboken där alla positioner från den nyckeln som har tagits fram hämtas ut och vilken färg det är.
                gameboard[pos.x, pos.y] = PieceInfo.setups[nrOfPlayers][i];
            }
        }
    }
}

private void InitPiecePos() //Den här metoden lägger in alla start positioner för varje färg i ordboken
{
    List<Position> p;
    if (startPiecePosition.Count > 0)
    {
        startPiecePosition.Clear();
    }

    for (int i = 0; i < 6; i++) 
    {
        switch (PieceInfo.setups[6][i]) 
        {
            case Piece.Red:
                p = new List<Position>();
                
                p.Add(new Position(-4, 8)); 

                p.Add(new Position(-4, 7)); 
                p.Add(new Position(-3, 7));

                p.Add(new Position(-4, 6));
                p.Add(new Position(-3, 6)); 
                p.Add(new Position(-2, 6));

                p.Add(new Position(-4, 5));
                p.Add(new Position(-3, 5));
                p.Add(new Position(-2, 5));
                p.Add(new Position(-1, 5));

                if (nrOfPlayers == 2)
                {
                p.Add(new Position(-4, 4));
                p.Add(new Position(-3, 4));
                p.Add(new Position(-2, 4));
                p.Add(new Position(-1, 4));
                p.Add(new Position(0,  4));
                }
                startPiecePosition.Add(Piece.Red, p); // Vi lägger till listan "p" i vår ordboks lista kopplad och dess färg.
                break;

            case Piece.Cyan:
                p = new List<Position>();
                p.Add(new Position(4, -8));
                p.Add(new Position(4, -7)); 
                p.Add(new Position(3, -7));

                p.Add(new Position(4, -6));
                p.Add(new Position(3, -6)); 
                p.Add(new Position(2, -6));

                p.Add(new Position(4, -5));
                p.Add(new Position(3, -5));
                p.Add(new Position(2, -5));
                p.Add(new Position(1, -5));

                if (nrOfPlayers == 2)
                {
                p.Add(new Position(4, -4));
                p.Add(new Position(3, -4));
                p.Add(new Position(2, -4));
                p.Add(new Position(1, -4));
                p.Add(new Position(0, -4));
                }
                startPiecePosition.Add(Piece.Cyan, p);
                break;

            case Piece.Green:
                p = new List<Position>();
                p.Add(new Position(8, -4));
                p.Add(new Position(7, -3)); 
                p.Add(new Position(7, -4));

                p.Add(new Position(6, -4));
                p.Add(new Position(6, -3)); 
                p.Add(new Position(5, -1));

                p.Add(new Position(5, -2));
                p.Add(new Position(5, -3));
                p.Add(new Position(5, -4));
                p.Add(new Position(6, -2));
                startPiecePosition.Add(Piece.Green, p);
                break;

            case Piece.Blue:
                p = new List<Position>();
                p.Add(new Position(-4, -4));
                p.Add(new Position(-4, -3)); 
                p.Add(new Position(-3, -4));

                p.Add(new Position(-2, -4));
                p.Add(new Position(-3, -3)); 
                p.Add(new Position(-4, -2));

                p.Add(new Position(-1, -4));
                p.Add(new Position(-2, -3));
                p.Add(new Position(-3, -2));
                p.Add(new Position(-4, -1));
                startPiecePosition.Add(Piece.Blue, p);
                break;

            case Piece.Magenta:
                p = new List<Position>();
                p.Add(new Position(-8, 4));
                p.Add(new Position(-7, 3)); 
                p.Add(new Position(-7, 4));

                p.Add(new Position(-6, 2));
                p.Add(new Position(-6, 3)); 
                p.Add(new Position(-6, 4));

                p.Add(new Position(-5, 1));
                p.Add(new Position(-5, 2));
                p.Add(new Position(-5, 3));
                p.Add(new Position(-5, 4));
                startPiecePosition.Add(Piece.Magenta, p);
                break;

            case Piece.Yellow:
                p = new List<Position>();
                p.Add(new Position(4, 4));
                p.Add(new Position(4, 3)); 
                p.Add(new Position(3, 4));

                p.Add(new Position(2, 4));
                p.Add(new Position(3, 3)); 
                p.Add(new Position(4, 2));

                p.Add(new Position(1, 4));
                p.Add(new Position(2, 3));
                p.Add(new Position(3, 2));
                p.Add(new Position(4, 1));
                startPiecePosition.Add(Piece.Yellow, p);
                break;
        }
    }
}

private void InitInvalidPos() //Denna metod lägger in alla ogiltiga positioner i en lista och sedan anger det i brädan
{
    List<Position> invalidPositions = new List<Position>();

    /*Nedre högra hörnet*/
    invalidPositions.Add(new Position(5, -8));
    invalidPositions.Add(new Position(5, -7));
    invalidPositions.Add(new Position(5, -6));
    invalidPositions.Add(new Position(5, -5));

    invalidPositions.Add(new Position(6, -8));
    invalidPositions.Add(new Position(6, -7));
    invalidPositions.Add(new Position(6, -6));
    invalidPositions.Add(new Position(6, -5));

    invalidPositions.Add(new Position(7, -8));
    invalidPositions.Add(new Position(7, -7));
    invalidPositions.Add(new Position(7, -6));
    invalidPositions.Add(new Position(7, -5));

    invalidPositions.Add(new Position(8, -8));
    invalidPositions.Add(new Position(8, -7));
    invalidPositions.Add(new Position(8, -6));
    invalidPositions.Add(new Position(8, -5));
    /*--------------------------*/

    /*Nedre vänstra hörnet */
    invalidPositions.Add(new Position(0, -8));
    invalidPositions.Add(new Position(0, -7));
    invalidPositions.Add(new Position(0, -6));
    invalidPositions.Add(new Position(0, -5));

    invalidPositions.Add(new Position(1, -8));
    invalidPositions.Add(new Position(1, -7));
    invalidPositions.Add(new Position(1, -6));
    invalidPositions.Add(new Position(-1, -8));
    invalidPositions.Add(new Position(-1, -7));
    invalidPositions.Add(new Position(-1, -6));
    invalidPositions.Add(new Position(-1, -5));

    invalidPositions.Add(new Position(2, -7));
    invalidPositions.Add(new Position(-2, -5));
    invalidPositions.Add(new Position(-2, -6));
    invalidPositions.Add(new Position(-2, -7));
    invalidPositions.Add(new Position(-2, -8));
    invalidPositions.Add(new Position(2, -8));

    invalidPositions.Add(new Position(-3, -5));
    invalidPositions.Add(new Position(-3, -6));
    invalidPositions.Add(new Position(-3, -7));
    invalidPositions.Add(new Position(3, -8));

    invalidPositions.Add(new Position(-4, -5));
    /*--------------------------*/

    /*Övre högra hörnet*/
    invalidPositions.Add(new Position(-3, 8));
    invalidPositions.Add(new Position(-2, 8));
    invalidPositions.Add(new Position(-1, 8));
    invalidPositions.Add(new Position(0, 8));
    invalidPositions.Add(new Position(1, 8));
    invalidPositions.Add(new Position(2, 8));

    invalidPositions.Add(new Position(-2, 7));
    invalidPositions.Add(new Position(-1, 7));
    invalidPositions.Add(new Position(0, 7));
    invalidPositions.Add(new Position(1, 7));
    invalidPositions.Add(new Position(2, 7));
    invalidPositions.Add(new Position(3, 7));

    invalidPositions.Add(new Position(-1, 6));
    invalidPositions.Add(new Position(0, 6));
    invalidPositions.Add(new Position(0, 5));

    invalidPositions.Add(new Position(1, 5));
    invalidPositions.Add(new Position(2, 5));
    invalidPositions.Add(new Position(3, 5));
    invalidPositions.Add(new Position(4, 5));

    invalidPositions.Add(new Position(1, 6));
    invalidPositions.Add(new Position(2, 6));
    invalidPositions.Add(new Position(3, 6));
    /*--------------------------*/


    /*Övre vänstra hörnet*/


    invalidPositions.Add(new Position(-5, 5));
    invalidPositions.Add(new Position(-5, 6));
    invalidPositions.Add(new Position(-5, 7));
    invalidPositions.Add(new Position(-5, 8));

    invalidPositions.Add(new Position(-6, 8));
    invalidPositions.Add(new Position(-6, 7));
    invalidPositions.Add(new Position(-6, 6));
    invalidPositions.Add(new Position(-6, 5));

    invalidPositions.Add(new Position(-7, 8));
    invalidPositions.Add(new Position(-7, 7));
    invalidPositions.Add(new Position(-7, 6));
    invalidPositions.Add(new Position(-7, 5));

    invalidPositions.Add(new Position(-8, 8));
    invalidPositions.Add(new Position(-8, 7));
    invalidPositions.Add(new Position(-8, 6));
    invalidPositions.Add(new Position(-8, 5));
    /*--------------------------*/

    /* Höger om mitten */

    invalidPositions.Add(new Position(5, 0));
    invalidPositions.Add(new Position(5, 1));
    invalidPositions.Add(new Position(5, 2));
    invalidPositions.Add(new Position(5, 3));
    invalidPositions.Add(new Position(5, 4));

    invalidPositions.Add(new Position(6, 0));
    invalidPositions.Add(new Position(6, 1));
    invalidPositions.Add(new Position(6, 2));
    invalidPositions.Add(new Position(6, -1));

    invalidPositions.Add(new Position(7, -1));
    invalidPositions.Add(new Position(7, -2));

    invalidPositions.Add(new Position(8, -2));
    invalidPositions.Add(new Position(8, -3));
    /*--------------------------*/

    /*Vänstra om mitten*/
    invalidPositions.Add(new Position(-5, 0));
    invalidPositions.Add(new Position(-5, -1));
    invalidPositions.Add(new Position(-5, -2));
    invalidPositions.Add(new Position(-5, -3));
    invalidPositions.Add(new Position(-5, -4));

    invalidPositions.Add(new Position(-6, 0));
    invalidPositions.Add(new Position(-6, 1));
    invalidPositions.Add(new Position(-6, -1));
    invalidPositions.Add(new Position(6, 2));
    invalidPositions.Add(new Position(6, -1));

    invalidPositions.Add(new Position(7, -1));
    invalidPositions.Add(new Position(-7, 1));
    invalidPositions.Add(new Position(-7, 2));
    invalidPositions.Add(new Position(7, 2));

    invalidPositions.Add(new Position(8, -2));
    invalidPositions.Add(new Position(8, -3));
    invalidPositions.Add(new Position(-8, 3));
    /*--------------------------*/


    foreach (var pos in invalidPositions)
    {
        gameboard[pos.x, pos.y] = Piece.Invalid;
    }

}

private void InitPlayers()
{
    if (players.Count > 0)
    {
        players.Clear();
    }

    players.Add(new Player(PieceInfo.setups[nrOfPlayers][0], true));
    for (int i = 1; i < nrOfPlayers; i++)
    {
        players.Add(new Player(PieceInfo.setups[nrOfPlayers][i], false));
    }

}


public List<Position> FindAllJumpPositions(Position startPos, List<Position> allJumpPositions) //Denna metod letar efter alla möjliga hopp positioner som en pjäs kan göra (Skrev denna metod med hjälp av Olle)
{
    foreach (Position direction in directions) //Går igenom alla riktingar
    {
        Position currentDirection = startPos + direction; //Adderar start positionen med den riktningen 
        if (currentDirection.x > xMax || currentDirection.x < xMin || currentDirection.y < yMin || currentDirection.y > yMax) //Ifall nya positionen är utanför spelbrädet går vi vidare till nästa index
        {
            continue;
        }

        if (gameboard[currentDirection.x, currentDirection.y] != Piece.Invalid && gameboard[currentDirection.x, currentDirection.y] != Piece.Empty)  //Om nya positionen inte är "piece.Invalid" och inte är tom, alltså att det står en pjäs på den positionen
        {
            Position newJumpPos = currentDirection + direction;
            if (newJumpPos.x > xMax || newJumpPos.x < xMin || newJumpPos.y < yMin || newJumpPos.y > yMax) //Ifall nya positionen är utanför spelbrädet går vi vidare till nästa index
            {
                continue;
            }

            if (gameboard[newJumpPos.x, newJumpPos.y] == Piece.Empty) //Ifall positionen bakom är tom placerar vi den positionen i listan och kallar på metoden igen men denna gång från den positionen vi avslutade på
            {
                if (!allJumpPositions.Contains(newJumpPos))
                {
                    allJumpPositions.Add(newJumpPos);
                    FindAllJumpPositions(newJumpPos, allJumpPositions);
                }
            }
        }
    }

    return allJumpPositions; // Returnerar en lista av alla möjliga positioner som går att hoppa till 
}


public List<Position> FindAllRegularPositions(Position pos, List<Position> availableMoves) //I denna metod kollas det efter vilka av dem sex positionerna runt om pjäsen är tom 
{
    foreach (Position direction in directions) //Går igenom alla riktingar
    {
        Position currentDirection = pos + direction; //Adderar start positionen med den riktningen 
        if (currentDirection.x > xMax || currentDirection.x < xMin || currentDirection.y < yMin || currentDirection.y > yMax) //Ifall nya positionen är utanför spelbrädet går vi vidare till nästa index
        {
            continue;
        }

        if (gameboard[currentDirection.x, currentDirection.y] == Piece.Empty) //Om positionen är tom och placerar vi den positionen i listan
        {
            if (!availableMoves.Contains(currentDirection))
            {
                availableMoves.Add(currentDirection);
            }
        }
    }
    return availableMoves; //Returnerar en lista av alla möjliga positioner som är tomma runt om
}

public Dictionary<Piece, List<Position>> GetStartPosition()
{
    return startPiecePosition;
}

public Piece[,] GetOldBoard()
{
    return gameboard;
}

public int GetNrOfPlayers()
{
    return nrOfPlayers;
}

private void WinCheck(Piece piece) //Kollar ifall spelaren eller AI:n har vunnit 
    {
        int playerIndex = 0;
        int redCounter = 0, cyanCounter = 0, blueCounter = 0, yellowCounter = 0, magentaCounter = 0, greenCounter = 0;
        List<int> everyCounter = new List<int>();

        for (int x = xMin; x <= xMax; x++) 
        {
            for (int y = yMin; y <= yMax; y++)
            {
                Position pos = new Position(x, y);
                if (gameboard[x, y] == piece)
                {
                    if (startPiecePosition[PieceInfo.opposites[(int)piece]].Contains(pos)) //Går igenom start positionerna i ordboken och kollar ifall pjäsen står i det motsatta färgen (målet), om den gör det plussas räknaren.
                    {
                        if (piece == Piece.Red)
                            redCounter++;
                        if (piece == Piece.Cyan)
                            cyanCounter++;
                        if (piece == Piece.Blue)
                            blueCounter++;
                        if (piece == Piece.Yellow)
                            yellowCounter++;
                        if (piece == Piece.Magenta)
                            magentaCounter++;
                        if (piece == Piece.Green)
                            greenCounter++;
                    }
                }
            }
        }
        everyCounter.Add(redCounter); everyCounter.Add(cyanCounter); everyCounter.Add(blueCounter); //lägger in alla räknare i en lista
        everyCounter.Add(yellowCounter); everyCounter.Add(magentaCounter); everyCounter.Add(greenCounter);

        for (int i = 0; i < everyCounter.Count; i++) //Går igenom alla räknare och kollar ifall en pjäs har alla 10/15 pjäser i målet. 
        {
            if (everyCounter[i] == nrOfPieces)
            {
                for (int j = 0; j < nrOfPlayers; j++)
                {
                    if (players[j].Value() == piece)
                    {
                        playerIndex = j;
                    }
                }

                foreach (var listener in listeners) //kallar på denna metod för att sätta en vinnare
                {
                    listener.SetNewWinner(players[playerIndex]);
                }
            }
        }
       
    }



}
