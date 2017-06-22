import subprocess
import logging
from JobInstance import JobInstance

class JobMonitor(object):
    """description of class"""
    def __init__(self, queueName):
        self.queueName = queueName

    def GetJobs(self):        
        res = subprocess.check_output(['qstat','-a'])
        jobs = []
        if res:
            job_result = str(res, encoding='utf-8')
            # filter out jobs for the specified queue
            matching = [s for s in job_result.splitlines() if self.queueName in s]
            for jobline in matching:
                logging.info(jobline)
                j = self.parseJobLine(jobline)
                jobs.append(j)
                logging.info('job ' + j.Id + ' parsed')
        return jobs

    def parseJobLine(self, jobline) -> JobInstance:
        # Job ID          Username Queue    Jobname    SessID NDS TSK Memory Time  S Time
        # 1153.pintamaste hpcuser  pinta00  LeMans_100    --    4  64    --    --  Q   --
        words = jobline.split()
        id = words[0].split('.')[0]
        job = JobInstance(id)
        job.QueueName = self.queueName
        job.SchedulerId = id
        job.Nodes = words[5]
        job.Status = self.map_job_status(words[9])
        return job


    def map_job_status(self, job_status):
        if job_status == 'F':
            return 'Finished'
        elif job_status == 'H':
            return 'Hold'
        elif job_status == 'Q':
            return 'Queued'
        elif job_status == 'E':
            return 'Failed'
        elif job_status == 'R':
            return 'Running'
        else:
            return 'Unknown'