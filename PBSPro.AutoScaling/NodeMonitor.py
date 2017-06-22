import subprocess
import logging
from NodeInstance import NodeInstance

class NodeMonitor(object):
    """description of class"""
    def __init__(self, queueName):
        self.queueName = queueName

    def GetNodes(self):        
        res = subprocess.check_output(['pbsnodes','-a','-F', 'dsv', '-L', '-S'])
        nodes = []
        if res:
            result = str(res, encoding='utf-8')
            # filter out nodes for the specified queue
            matching = [s for s in result.splitlines() if self.queueName in s]
            for line in matching:
                logging.info(line)
                n = self.parseNodeLine(line)
                nodes.append(n)
                logging.info('node ' + n.Name + ' parsed')
        return nodes

    def parseNodeLine(self, line) -> NodeInstance:
        # vnode=pinta00s2000002|state=job-busy|OS=--|hardware=--|host=172-0-0-6|queue=pinta00|mem=110gb|ncpus=16|nmics=0|ngpus=0|comment=--
        words = line.split('|')
        name = words[0].split('=')[1]
        logging.info(name)
        node = NodeInstance(name)
        node.Name = name
        node.PoolName = self.queueName
        jobstatus = words[1].split('=')[1]
        logging.info(jobstatus)
        node.JobStatus = self.map_job_status(jobstatus)  
        return node


    def map_job_status(self, job_status):
        if job_status == 'free':
            return 'Free'
        elif job_status == 'job-busy':
            return 'Busy'
        else:
            return 'Unknown'
