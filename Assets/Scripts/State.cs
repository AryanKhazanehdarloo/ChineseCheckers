using System;
using System.Collections.Generic;
using UnityEngine;
using Minimax;
using Position = UnityEngine.Vector2Int;
using Coordinate = UnityEngine.Vector2;
public class State : IState
{
   private BoardModel boardModel;
   private Piece[,] gameBoard;
   private const int xMin = -8, yMin = -8, xMax = 8, yMax = 8;
    public State(Piece[,] gameBoard)
    {
        boardModel = BoardModel.Instance();
        this.gameBoard = gameBoard;
    }
  public List<IState> Expand(IPlayer player) //Kollar igenom alla möjliga drag för varje pjäs 
  {
    Position startPos = new Position();
    Position endPos = new Position();
    List<IState> output = new List<IState>();
    for (int x = xMin; x <= xMax; x++)
    {
        for (int y = yMin; y <= yMax; y++)
        {
            if (gameBoard[x, y] == player.Value()) 
            {
                startPos.Set(x, y);
                List<Position> allLegalMoves = new List<Position>();
                allLegalMoves.AddRange(boardModel.FindAllJumpPositions(startPos, new List<Position>())); 
                allLegalMoves.AddRange(boardModel.FindAllRegularPositions(startPos, new List<Position>()));

                foreach (var pos in allLegalMoves) //Uppdaterar brädet och sätter dem gamla positioerna som "empty"
                {
                    endPos.Set(pos.x, pos.y);
                    State nextState = new State((Piece[,])gameBoard.Clone());
                    Piece movingPiece = nextState.gameBoard[startPos.x, startPos.y];
                    nextState.gameBoard[startPos.x, startPos.y] = Piece.Empty;
                    nextState.gameBoard[endPos.x, endPos.y] = movingPiece;
                    output.Add(nextState); // Skickar nya brädet
                }
            }
        }
    }
        return output;
  }

  public int Score(IPlayer player)  //Logiken bakom poäng systemet är att försöka få AI:n att börja komma ut sin "spawn" sedan när den har nått halva spelbärdet börja ta sig fram till sin mål.
  {
        int score = 0;
        Piece[,] oldgameBoard = boardModel.GetOldBoard();
        int nrOfPlayers = boardModel.GetNrOfPlayers();

        Dictionary<Piece, List<Position>> everyStartPos = boardModel.GetStartPosition();
        List<Position> redStartPos, cyanStartPos, yellowStartPos, greenStartPos, blueStartPos, magentaStartPos;
        redStartPos = everyStartPos[Piece.Red]; cyanStartPos = everyStartPos[Piece.Cyan]; blueStartPos = everyStartPos[Piece.Blue];
        greenStartPos = everyStartPos[Piece.Green]; yellowStartPos = everyStartPos[Piece.Yellow]; magentaStartPos = everyStartPos[Piece.Magenta];

        Position cyanTarget = new Position(-3, 6); Position redTarget = new Position(3, -6); 
        Position greenTarget = new Position(-5, 3);Position blueTarget = new Position(2, 3); 
        Position magentaTarget = new Position(5, -3); Position yellowTarget = new Position(-3, -2);

        Position endPosition = new Position(0, 0);
        Position startPosition = new Position(0, 0);

        for (int x = xMin; x <= xMax; x++)  // Tar reda på pjäsens start och slut position genom att jämföra gamla och nya brädan
        {
            for (int y = yMin; y <= yMax; y++)
            {
                if (gameBoard[x, y] != oldgameBoard[x, y] && gameBoard[x, y] == player.Value())
                {
                    endPosition.Set(x, y);

                }
                else if (gameBoard[x, y] != oldgameBoard[x, y] && oldgameBoard[x, y] == player.Value())
                {
                    startPosition.Set(x, y);
                }
            }    
        }


        if (player.Value() == Piece.Cyan)
        {

            if (redStartPos.Contains(startPosition)) //Om pjäsen har nåt sitt mål så får den röra sig men den får inte mycket poäng
            {

                score = 1;
            }
            else
            {
                foreach (var item in cyanStartPos)
                {
                    if (startPosition == item && !cyanStartPos.Contains(endPosition)) //Om pjäsen står i en av sina start positioner men kan gå ut så får den poäng.
                    {
                        score = 3;
                    }

                    if (startPosition == item && cyanStartPos.Contains(endPosition)) //Om pjäsen står i en av sina start positioner men kan enbart röra sig runt så får den poäng (hjälper till att uppnå första if satsen)
                    {
                        score = 3;
                    }

                }

                if (Position.Distance(endPosition, cyanTarget) < 7) //Ifall en av pjäsens slutpositioner är närheten av sitt mål får den poäng för att leta sig fram
                {
                    score = 4;
                }

                if (yellowStartPos.Contains(endPosition) || magentaStartPos.Contains(endPosition)) //AI:n får inte mycket poäng ifall den går till dessa färger
                {
                    score = 1;
                }

                if (endPosition == new Position(startPosition.x + 1, startPosition.y) || (endPosition == new Position(startPosition.x - 1, startPosition.y))) //Pjäsen får mindre poäng om går åt sidan 
                {
                    score = 1;
                }

                if (endPosition == new Position(startPosition.x, startPosition.y - 1) || (endPosition == new Position(startPosition.x + 1, startPosition.y - 1))) //Pjäsen får mindre poäng om går ett steg bakåt 
                {
                    score = 1;
                }

                if (redStartPos.Contains(endPosition)) //Om pjäsens slut positioner är en av mål positionerna
                {
                    score = 5;
                }
            }
        }

        if (player.Value() == Piece.Green)
        {
            if (nrOfPlayers == 3)
            {
                if (redStartPos.Contains(startPosition))
                {

                    score = 0;
                }
                else
                {
                    foreach (var item in greenStartPos)
                    {
                        if (startPosition == item && !greenStartPos.Contains(endPosition))
                        {
                            score = 4;
                        }

                        if (startPosition == item && greenStartPos.Contains(endPosition))
                        {
                            score = 3;
                        }
                    }

                    if (Position.Distance(endPosition, cyanTarget) < 6)
                    {
                        score = 3;
                    }

                    if (cyanStartPos.Contains(endPosition) || magentaStartPos.Contains(endPosition))
                    {
                        score = 1;
                    }

                    if (endPosition == new Position(startPosition.x + 1, startPosition.y)) //Pjäsen får mindre poäng om går ett steg bakåt 
                    {
                        score = 1;
                    }

                    if (redStartPos.Contains(endPosition))
                    {
                        score = 5;
                    }
                }
            }

            if (nrOfPlayers != 3)
            {
                if (magentaStartPos.Contains(startPosition))
                {

                    score = 0;
                }
                else
                {
                    foreach (var item in greenStartPos)
                    {
                        if (startPosition == item && !greenStartPos.Contains(endPosition))
                        {
                            score = 4;
                        }

                        if (startPosition == item && greenStartPos.Contains(endPosition))
                        {
                            score = 3;
                        }
                    }

                    if (Position.Distance(endPosition, greenTarget) < 7)
                    {
                        score = 4;
                    }

                    if (redStartPos.Contains(endPosition) || blueStartPos.Contains(endPosition) || yellowStartPos.Contains(endPosition))
                    {
                        score = 1;
                    }

                    if (endPosition == new Position(startPosition.x - 1, startPosition.y))
                    {
                        score = 2;
                    }

                    if (magentaStartPos.Contains(endPosition))
                    {
                        score = 5;
                    }
                }
            }

        }

        if (player.Value() == Piece.Blue)
        {
            if (nrOfPlayers == 3)
            {
                if (greenStartPos.Contains(startPosition))
                {

                    score = 1;
                }
                else
                {
                    foreach (var item in blueStartPos)
                    {
                        if (startPosition == item && !blueStartPos.Contains(endPosition))
                        {
                            score = 4;
                        }
                        if (startPosition == item && blueStartPos.Contains(endPosition))
                        {
                            score = 3;
                        }
                    }

                    if (endPosition == new Position(startPosition.x - 1, startPosition.y)) //Pjäsen får mindre poäng om går ett steg bakåt 
                    {
                        score = 1;
                    }


                    if (greenStartPos.Contains(endPosition))
                    {
                        score = 5;
                    }


                    if (Position.Distance(endPosition, magentaTarget) < 5)
                    {
                        score = 4;
                    }

                    if (Position.Distance(endPosition, magentaTarget) < 3)
                    {
                        score = 5;
                    }

                    if (magentaStartPos.Contains(endPosition) || yellowStartPos.Contains(endPosition))
                    {
                        score = 1;
                    }
                }

            }

            if (nrOfPlayers != 3)
            {
                if (yellowStartPos.Contains(startPosition))
                {

                    score = 1;
                }
                else
                {
                    foreach (var item in blueStartPos)
                    {
                        if (startPosition == item && !blueStartPos.Contains(endPosition))
                        {
                            score = 4;
                        }
                        if (startPosition == item && blueStartPos.Contains(endPosition))
                        {
                            score = 3;
                        }
                    }

                    if (endPosition == new Position(startPosition.x - 1, startPosition.y)) //Pjäsen får mindre poäng om går ett steg bakåt 
                    {
                        score = 1;
                    }


                    if (yellowStartPos.Contains(endPosition))
                    {
                        score = 5;
                    }


                    if (Position.Distance(endPosition, blueTarget) < 5)
                    {
                        score = 4;
                    }

                    if (Position.Distance(endPosition, blueTarget) < 3)
                    {
                        score = 5;
                    }

                    if (redStartPos.Contains(endPosition) || greenStartPos.Contains(endPosition))
                    {
                        score = 1;
                    }
                }


            }

        }

        if (player.Value() == Piece.Magenta)
        {
            if (greenStartPos.Contains(startPosition))
            {

                score = 1;
            }
            else
            {
                foreach (var item in magentaStartPos)
                {
                    if (startPosition == item && !magentaStartPos.Contains(endPosition))
                    {
                        score = 4;
                    }
                    //Får ut dem från spawn
                    if (startPosition == item && magentaStartPos.Contains(endPosition))
                    {
                        score = 3;
                    }
                }
                if (Position.Distance(endPosition, magentaTarget) < 6)
                {
                    score = 4;
                }

                if (yellowStartPos.Contains(endPosition) || cyanStartPos.Contains(endPosition))
                {
                    score = 1;
                }

                if (endPosition == new Position(startPosition.x - 1, startPosition.y)) //Pjäsen får mindre poäng om går ett steg bakåt 
                {
                    score = 1;
                }

                if (greenStartPos.Contains(endPosition))
                {
                    score = 5;
                }
            }
        }

        if (player.Value() == Piece.Yellow)
        {
            if (blueStartPos.Contains(startPosition))
            {
                score = 1;
            }
            else
            {
                foreach (var item in yellowStartPos)
                {

                    if (startPosition == item && !yellowStartPos.Contains(endPosition))
                    {
                        score = 4;
                    }

                    if (startPosition == item && yellowStartPos.Contains(endPosition))
                    {
                        score = 3;
                    }
                }

                if (blueStartPos.Contains(endPosition))
                {
                    score = 5;
                }

                if (Position.Distance(endPosition, yellowTarget) < 6)
                {
                    score = 4;
                }

                if (magentaStartPos.Contains(endPosition) || cyanStartPos.Contains(endPosition))
                {
                    score = 1;
                }

                if (endPosition == new Position(startPosition.x + 1, startPosition.y))
                {
                    score = 1;
                }
            }

        }

        if (player.Value() == Piece.Red)
        {
            if (cyanStartPos.Contains(startPosition))
            {
                score = 0;
            }
            else
            {
                foreach (var item in redStartPos)
                {
                    if (startPosition == item && !redStartPos.Contains(endPosition))
                    {
                        score = 4;
                    }
                    //Får ut dem från spawn
                    if (startPosition == item && redStartPos.Contains(endPosition))
                    {
                        score = 2;
                    }
                }

                if (cyanStartPos.Contains(endPosition))
                {
                    score = 5;
                }

                if (Position.Distance(endPosition, redTarget) < 7)
                {
                    score = 4;
                }

                if (blueStartPos.Contains(endPosition) || greenStartPos.Contains(endPosition))
                {
                    score = 1;
                }
            }
        }

        //Poäng logiken för varje färg

        return score;
  }
 
  public Piece[,] GetGameBoard()
  {
    return gameBoard;
  }

}