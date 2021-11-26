# -*- coding: utf-8 -*-
"""
Modelos de Agente y del medio ambiente
Movimiento aleatorio del auto en el grid

Solución al reto de TC2008B semestre AgostoDiciembre 2021
Autor: Jorge Ramírez Uresti, Octavio Navarro
"""

from mesa import Agent, Model
from mesa.time import BaseScheduler, RandomActivation
from mesa.space import SingleGrid
import math

class RobotAgent(Agent):
    """
    Agent that moves randomly.
    Attributes:
        unique_id: Agent's ID 
        direction: Randomly chosen direction chosen from one of eight directions
    """
    def __init__(self, unique_id, model):
        """
        Creates a new random agent.
        Args:
            unique_id: The agent's ID
            model: Model reference for the agent
        """
        super().__init__(unique_id, model)
        self.has_box = False
        self.tag = "robot"

    def move(self):
        pass
    def step(self):
        """ 
        Determines if the agent can move in the direction that was chosen
        """
        print(f"I, agent {self.unique_id} am in position ({self.pos[0]}, {self.pos[1]}) at the beginning of the step.")

        possible_steps = self.model.grid.get_neighborhood(
            self.pos,
            moore = False, # Boolean for whether to use Moore neighborhood (including diagonals) or Von Neumann (only up/down/left/right).
            include_center = False)
        #print("possible steps", possible_steps)

        box = None
        presentInCell = self.model.grid.get_cell_list_contents(possible_steps)
        #print("presentInCell", presentInCell)
        #print("present in cell has ", len(presentInCell), "entries")
        for agent in presentInCell:
            if(agent.tag == "box"):
                box = agent

        if(self.has_box and self.model.drop_zone in possible_steps):
            # Dejar caja
            print(f"I, agent {self.unique_id} can drop the box, I am doing it\n")
            self.model.boxes_dropped += 1
            self.has_box = False
            return

        elif(box and not self.has_box):
            #paso 2
            box.picked_up = True
            self.model.grid.remove_agent(box)
            self.has_box = True
            return

        
        freeSpaces = list(map(self.model.grid.is_cell_empty, possible_steps))
        print(f"I agent {self.unique_id} have neighborhood {possible_steps}")
        print(f"The ones that are empty are: {freeSpaces}")
        cell_to_move = None
        empty_positions = []
        for i in range(0, len(possible_steps)):
            list_with_agent_in_cell = self.model.grid.get_cell_list_contents([possible_steps[i]])
            if freeSpaces[i] == True:  # If theres an empty cell or a box cell thats picked up.
                empty_positions.append(possible_steps[i])
            elif list_with_agent_in_cell[0].tag == "box":
                if list_with_agent_in_cell[0].picked_up == True:
                    empty_positions.append(possible_steps[i])
                
        
        if(self.has_box):
            #paso 3
            print(f"I, agent {self.unique_id} have box but cant drop it yet, finding best route\n")
            print(f"My possible steps: are {empty_positions}")

            distance = math.inf
            index_min_distance = None
            for index, cell in enumerate(empty_positions):
                distance_from_cell = math.sqrt((cell[0] - self.model.drop_zone[0])**2+(cell[1] - self.model.drop_zone[1])**2)
                print(f"The distance from cell {cell} to drop zone {self.model.drop_zone} is {distance_from_cell}")
                print(f"The min distance so far is {distance}, its index in the empty positions is {index_min_distance}")
                if(distance_from_cell < distance):
                    distance = distance_from_cell
                    index_min_distance = index

            if(isinstance(index_min_distance, int)):
                cell_to_move = empty_positions[index_min_distance]
                print(f"I will move to cell {cell_to_move}")

                #print("The empty position with the least distance to the drop zone is: {empty_positions[index_min_distance]}")

        else:
            cell_to_move = self.model.random.choice(empty_positions)
            print(f"I, agent {self.unique_id} chose to move to random empty {cell_to_move}")

        # If the cell is empty, moves the agent to that cell; otherwise, it stays at the same position
        if cell_to_move:
            print(f"El agente {self.unique_id} mueve de {self.pos}", end = " ")
            self.model.total_moves += 1

            #self.model.grid.remove_agent(self)

            # AAAAAAAAAAAA AQUI ESTA LA SALSA.
            # The agents were trying to move into positions of boxes that we couldnt see they were picked up single grid crashed.
            self.model.grid.move_agent(self, cell_to_move)

            """
            list_with_agent_in_cell_to_move = self.model.grid.get_cell_list_contents(cell_to_move)

            if self.model.grid.is_cell_empty(cell_to_move):    
                self.model.grid.move_agent(self, cell_to_move)
            elif list_with_agent_in_cell_to_move[0].tag == "box":
                if list_with_agent_in_cell_to_move[0].picked_up == True:
                    self.model.grid.move_agent(self, cell_to_move)
            """
            print(f"a {cell_to_move}")                
        else:
            print(f"El agente {self.unique_id} no se puede mover de {self.pos}. No hay celdas vacias")


class ObstacleAgent(Agent):
    """
    Obstacle agent. Just to add obstacles to the grid.
    """
    def __init__(self, unique_id, model, tag):
        super().__init__(unique_id, model)
        self.tag = tag
        self.picked_up = False

    def step(self):
        pass   

class RobotModel(Model):
    """ 
    Creates a new model with random agents.
    Args:
        N: Number of agents in the simulation
        height, width: The size of the grid to model
    """
    def __init__(self, N, max_shelves, N_boxes, width, height):
        self.num_agents = max([5,N])
        self.shelves = self.random.randrange(max_shelves)
        self.num_boxes = N_boxes
        self.grid = SingleGrid(width,height,torus = False) 
        self.schedule = BaseScheduler(self)
        self.running = True
        self.drop_zone = (self.random.randrange(1, self.grid.width - 1), 
                          self.random.randrange(1, self.grid.height -1))
        self.boxes_dropped = 0
        self.cant_steps = 0
        self.total_moves = 0

        # Creates the border of the grid
        border = [(x,y) for y in range(height) for x in range(width) if y in [0, height-1] or x in [0, width - 1]]

        # Add the barriers at the border of existing grid, not outside of it.
        for ind, pos in enumerate(border):
            obs = ObstacleAgent(ind, self, "border")
            self.grid.place_agent(obs, pos)

        # Add shelves to a random empty grid cell
        for i in range(self.shelves):
            obj = ObstacleAgent(i, self, "shelf")

            pos_gen = lambda w, h: (self.random.randrange(1, w - 1), self.random.randrange(1, h - 1))
            pos = pos_gen(self.grid.width, self.grid.height)
            while (not self.grid.is_cell_empty(pos)):
                pos = pos_gen(self.grid.width, self.grid.height)
            self.grid.place_agent(obj, pos)

        # Add boxes to a random empty grid cell
        for i in range(self.num_boxes):
            obj = ObstacleAgent(i, self, "box")

            pos_gen = lambda w, h: (self.random.randrange(1, w - 1), self.random.randrange(1, h - 1))
            pos = pos_gen(self.grid.width, self.grid.height)
            while (not self.grid.is_cell_empty(pos)):
                pos = pos_gen(self.grid.width, self.grid.height)
            self.grid.place_agent(obj, pos)

        # Add the agent to a random empty grid cell
        for i in range(self.num_agents):
            a = RobotAgent(i+1000, self) 
            self.schedule.add(a)

            pos_gen = lambda w, h: (self.random.randrange(1, w - 1), self.random.randrange(1, h - 1))
            pos = pos_gen(self.grid.width, self.grid.height)
            while (not self.grid.is_cell_empty(pos)):
                pos = pos_gen(self.grid.width, self.grid.height)
            self.grid.place_agent(a, pos)

    def step(self):
        '''Advance the model by one step.'''
        if(self.boxes_dropped < self.num_boxes):
            self.schedule.step()
            self.cant_steps += 1
        else:
            print(f"FINISHED\nTotal steps: {self.cant_steps}")
            print(f"FINISHED\nTotal moves: {self.total_moves}")
