import time
import logging
import logging.config
import logging.handlers
from JobMonitor import JobMonitor
import config as config
from ClusterLoadRepository import ClusterLoadRepository
from NodeMonitor import NodeMonitor
import os

realPath = os.path.realpath(__file__)
dirPath = os.path.dirname(realPath)

logging.config.fileConfig(dirPath + '/log.conf')
logging.info('starting new session')

while True:
    try:
        loadrepo = ClusterLoadRepository(config.DOCUMENTDB_ENDPOINT, config.DOCUMENTDB_AUTHKEY, config.DOCUMENTDB_DATABASE, config.DOCUMENTDB_COLLECTION)
        queues = config.QUEUE_LIST.split(',')

        for queueName in queues:
            logging.info("processing queue " + queueName)
            # cleanup jobs
            logging.info("cleaning jobs")
            jobs = loadrepo.ListActiveJobs(queueName)
            for j in jobs:
                loadrepo.DeleteDocument(j)
            logging.info(str(len(jobs)) + " jobs deleted")

            # cleanup nodes
            logging.info("cleaning nodes")
            nodes = loadrepo.ListActiveNodes(queueName)
            for n in nodes:
                loadrepo.DeleteDocument(n)
            logging.info(str(len(nodes)) + " nodes deleted")

            jobmonitor = JobMonitor(queueName)
            jobs = jobmonitor.GetJobs()
            logging.info(str(len(jobs)) + " jobs listed")
            for j in jobs:
                loadrepo.UpdateDocument(j._dic)

            nodemonitor = NodeMonitor(queueName)
            nodes = nodemonitor.GetNodes()
            logging.info(str(len(nodes)) + " nodes listed")
            for n in nodes:
                loadrepo.UpdateDocument(n._dic)

    except Exception:
        logging.exception("message")
    finally:
        logging.info("wait 2mn")
        time.sleep(120)
