# -*- coding: utf-8 -*-
"""
Modelos de Agente y del medio ambiente
Movimiento aleatorio del auto en el grid

Model of the Agent and the enviornment.
Random movement & path to return home from the robot agent on the grid.

Solution to the situational problem TC2008B August-December 2021
Autors: Jorge Ramírez Uresti, Octavio Navarro, Luis Ignacio Ferro Salinas, Joan Daniel Guerrero García, Daniel García Barajas
"""

from mesa import Agent, Model
from mesa.time import BaseScheduler
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

    def step(self):
        """ 
        Determines if the agent can move in the direction that was chosen
        """
        possible_steps = self.model.grid.get_neighborhood(
            self.pos,
            moore = False, # Boolean for whether to use Moore neighborhood (including diagonals) or Von Neumann (only up/down/left/right).
            include_center = False)

        box = None
        presentInCell = self.model.grid.get_cell_list_contents(possible_steps)
        for agent in presentInCell:
            if(agent.tag == "box"):
                box = agent

        if(self.has_box and self.model.drop_zone in possible_steps):
            # Leave box
            self.model.boxes_dropped += 1
            self.has_box = False
            return

        elif(box and not self.has_box):
            # Pick up box
            box.picked_up = True
            self.model.grid.remove_agent(box)
            self.has_box = True
            return

        freeSpaces = list(map(self.model.grid.is_cell_empty, possible_steps))
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
            # Return to drop zone
            distance = math.inf
            index_min_distance = None
            for index, cell in enumerate(empty_positions):
                distance_from_cell = math.sqrt((cell[0] - self.model.drop_zone[0])**2+(cell[1] - self.model.drop_zone[1])**2)
                if(distance_from_cell < distance):
                    distance = distance_from_cell
                    index_min_distance = index

            if(isinstance(index_min_distance, int)):
                cell_to_move = empty_positions[index_min_distance]

        else:
            cell_to_move = self.model.random.choice(empty_positions)

        # If the cell is empty, moves the agent to that cell; otherwise, it stays at the same position
        if cell_to_move:
            print(f"El agente {self.unique_id} se mueve de {self.pos}", end = " ")
            self.model.total_moves += 1
            # The agents were trying to move into positions of boxes that we couldnt see they were picked up single grid crashed.
            self.model.grid.move_agent(self, cell_to_move)
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
    def __init__(self, N, max_shelves, N_boxes, width, height, max_moves):
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
        self.max_moves = max_moves

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
        if(self.boxes_dropped < self.num_boxes and self.max_moves > self.cant_steps):
            self.schedule.step()
            self.cant_steps += 1
        else:
            print(f"FINISHED\nTotal steps: {self.cant_steps}")
            print(f"FINISHED\nTotal moves: {self.total_moves}")
