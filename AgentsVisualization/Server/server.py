# TC2008B. Sistemas Multiagentes y Gráficas Computacionales
# Python flask server to interact with Unity. Based on the code provided by Sergio Ruiz.
# Octavio Navarro. October 2021

from flask import Flask, request, jsonify
from RobotAgents import *

# Size of the board:

"""
number_agents = input("Number of agents: ")
max_shelves = input("Maximum number of shelves: ") # If there's time make rows.
number_boxes = input("Number of boxes: ")
width = input("Width of the grid: ")
height = input("Height of the grid: ")
warehouse_model = None
currentStep = 0
"""


number_agents = 10
max_shelves = 5
number_boxes = 10
width = 28
height = 28
warehouse_model = None
currentStep = 0

app = Flask("Warehouse example")

# @app.route('/', methods=['POST', 'GET'])

@app.route('/init', methods=['POST', 'GET'])
def initModel():
    global currentStep, warehouse_model, number_agents, max_shelves, number_boxes, width, height

    if request.method == 'POST':
        number_agents = int(request.form.get('NAgents'))
        number_boxes = int(request.form.get('NBoxes'))
        width = int(request.form.get('width'))
        height = int(request.form.get('height'))
        max_shelves = int(request.form.get('maxShelves'))
        currentStep = 0

        print(request.form)
        print(number_agents, max_shelves, number_boxes, width, height)
        warehouse_model = RobotModel(number_agents, max_shelves, number_boxes, width, height)
        print("MODEL: ", warehouse_model)

        return jsonify({"message":"Parameters recieved, model initiated."})

@app.route('/getRobotAgents', methods=['GET'])
def getAgents():
    global warehouse_model

    if request.method == 'GET':
        robots_attributes = [{"x": x, "y": 1, "z": z, "has_box": a.has_box} for (a, x, z) in warehouse_model.grid.coord_iter() if isinstance(a, RobotAgent)]
        #print("Robot count: ", len(robots_attributes))
        return jsonify({'robots_attributes': robots_attributes})

@app.route('/getObstacles', methods=['GET'])
def getObstacles():
    global warehouse_model

    if request.method == 'GET':
        obstaclePositions = [{"x": x, "y":1, "z":z, "tag":a.tag, "picked_up": a.picked_up}  for (a, x, z) in warehouse_model.grid.coord_iter() if isinstance(a, ObstacleAgent)]
        # Get tag(s) and add to jsonify
        return jsonify({'obstacles_attributes':obstaclePositions})

@app.route('/getDroppedBoxes', methods=['GET'])
def getDroppedBoxes():
    global warehouse_model

    if request.method == 'GET':
        return jsonify({'droppedBoxes': warehouse_model.boxes_dropped})

@app.route('/update', methods=['GET'])
def updateModel():
    global currentStep, warehouse_model
    if request.method == 'GET':
        warehouse_model.step()
        currentStep += 1
        return jsonify({'message':f'Model updated to step {currentStep}.', 'currentStep':currentStep})

if __name__=='__main__':
    app.run(host="localhost", port=8585, debug=True)