class NodeInstance(object):
    """description of class"""
    def __init__(self, id):
        self._dic = {}
        self._dic["Type"] = "vms"
        self.Id = id

    @property
    def Id(self):
        return self._dic['id']
    @Id.setter
    def Id(self, value):
        self._dic['id'] = value

    @property
    def Name(self):
        return self._dic['Name']
    @Name.setter
    def Name(self, value):
        self._dic['Name'] = value

    @property
    def PoolName(self):
        return self._dic['PoolName']
    @PoolName.setter
    def PoolName(self, value):
        self._dic['PoolName'] = value

    @property
    def JobStatus(self):
        return self._dic['JobStatus']
    @JobStatus.setter
    def JobStatus(self, value):
        self._dic['JobStatus'] = value


