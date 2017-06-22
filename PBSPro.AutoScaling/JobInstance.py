class JobInstance(object):
    """description of class"""
    def __init__(self, id):
        self._dic = {}
        self._dic["Type"] = "jobs"
        self.Id = id

    @property
    def Id(self):
        return self._dic['id']
    @Id.setter
    def Id(self, value):
        self._dic['id'] = value

    @property
    def SchedulerId(self):
        return self._dic['SchedulerId']
    @SchedulerId.setter
    def SchedulerId(self, value):
        self._dic['SchedulerId'] = value

    @property
    def QueueName(self):
        return self._dic['QueueName']
    @QueueName.setter
    def QueueName(self, value):
        self._dic['QueueName'] = value

    @property
    def Nodes(self):
        return self._dic['Nodes']
    @Nodes.setter
    def Nodes(self, value):
        self._dic['Nodes'] = value

    @property
    def Status(self):
        return self._dic['Status']
    @Status.setter
    def Status(self, value):
        self._dic['Status'] = value
