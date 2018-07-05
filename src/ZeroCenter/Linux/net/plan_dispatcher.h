#pragma once
#ifndef _PLAN_DISPATCHER_H_
#define _PLAN_DISPATCHER_H_
#include "../stdinc.h"
#include "zero_plan.h"
#include "zero_station.h"
#include "inner_socket.h"

namespace agebull
{
	namespace zmq_net
	{
		/**
		* \brief ��ʾ�ƻ�������ȷ���
		*/
		class plan_dispatcher :public zero_station
		{
			friend plan_message;
			map<string, shared_ptr<inner_socket>> sockets_;
		public:
			/**
			* \brief ����
			*/
			static plan_dispatcher* instance;

			/**
			* \brief ����
			*/
			plan_dispatcher()
				:zero_station("PlanDispatcher", STATION_TYPE_PLAN, ZMQ_ROUTER)
			{

			}
			/**
			* \brief ����
			*/
			plan_dispatcher(shared_ptr<zero_config>& config)
				:zero_station(config, STATION_TYPE_PLAN, ZMQ_ROUTER)
			{

			}
			/**
			* \brief ����
			*/
			~plan_dispatcher() final = default;

			/**
			* \brief ����һ���㲥�߳�
			*/
			static void run()
			{
				instance = new plan_dispatcher();
				boost::thread(boost::bind(launch, shared_ptr<plan_dispatcher>(instance)));
			}
		private:
			/**
			*\brief ����
			*/
			static void run(shared_ptr<zero_config>& config)
			{
				instance = new plan_dispatcher(config);
				boost::thread(boost::bind(launch, shared_ptr<plan_dispatcher>(instance)));
			}
			/**
			* \brief ��Ϣ��
			*/
			static void launch(shared_ptr<plan_dispatcher>& station);


			/**
			* \brief �ƻ���ѯ
			*/
			static void run_plan_poll(plan_dispatcher* station)
			{
				station->plan_poll();
			}
			/**
			* \brief �ƻ���ѯ
			*/
			void plan_poll();

			/**
			* \brief ��ʼִ��һ������
			*/
			shared_char command(const char* caller, vector<shared_char> lines) final
			{
				return ZERO_STATUS_NET_ERROR;
			}

			/**
			*\brief ������Ϣ
			*/
			bool result_event(shared_ptr<plan_message>& message, vector<shared_char>& result);
			
			/**
			*\brief �㲥����
			*/
			bool zero_event(zero_net_event event_type, const plan_message* message);

			/**
			* \brief ������ʼ�����͵������ߣ�
			*/
			void job_start(ZMQ_HANDLE socket, vector<shared_char>& list, bool inner) final;
			/**
			* \brief ��������(���͵�������)
			*/
			void job_end(vector<shared_char>& list) final
			{
			}
			/**
			* \brief ִ�мƻ�
			*/
			void exec_plan(shared_ptr<plan_message>& msg);
			/**
			* \brief �ƻ�ִ�з���
			*/
			void on_plan_result(vector<shared_char>& list);
			/**
			* \brief �ƻ�ִ�з���
			*/
			void on_plan_result(shared_ptr<plan_message>& message, char state, vector<shared_char>& list);
			/**
			* \brief �ƻ�����
			*/
			bool on_plan_start(ZMQ_HANDLE socket, vector<shared_char>& list);
			/**
			* \brief �ƻ�����
			*/
			void on_plan_manage(ZMQ_HANDLE socket, vector<shared_char>& list);
			/**
			* \brief ִ������
			*/
			static char exec_command(const char* command, vector<shared_char>& arguments, string& json);
			/**
			* \brief �ƻ��б�
			*/
			static void plan_list(string& json);
		};
	}
}
#endif //!_PLAN_DISPATCHER_H_